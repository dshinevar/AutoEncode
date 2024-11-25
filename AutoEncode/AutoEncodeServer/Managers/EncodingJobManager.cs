using AutoEncodeServer.Communication;
using AutoEncodeServer.Factories;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;

namespace AutoEncodeServer.Managers;

// MAIN
public partial class EncodingJobManager :
    ManagerBase,
    IEncodingJobManager
{
    #region Dependencies
    public IEncodingJobModelFactory EncodingJobModelFactory { get; set; }

    private ISourceFileManager _sourceFileManager;
    #endregion Dependencies

    #region Private Properties
    private static readonly object _lock = new();
    private ulong _idNumber = 1;
    private ulong IdNumber
    {
        get
        {
            ulong tmp = _idNumber;
            _idNumber++;
            return tmp;
        }
    }

    private readonly ObservableCollection<IEncodingJobModel> _encodingJobQueue = [];
    #endregion Private Properties

    #region Public Properties
    public bool Initialized { get; private set; } = false;
    public int Count => _encodingJobQueue.Count;
    #endregion Public Properties

    /// <summary>Default Constructor</summary>
    public EncodingJobManager()
    {
        _encodingJobQueue.CollectionChanged += EncodingJobQueue_CollectionChanged;
    }

    #region Initialize / Start / Shutdown
    public override void Initialize(ManualResetEvent shutdownMRE)
    {
        if (Initialized is false)
        {
            try
            {
                ShutdownMRE = shutdownMRE;
                ShutdownMRE.Reset();

                _sourceFileManager = Container.Resolve<ISourceFileManager>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to initialize {nameof(EncodingJobManager)}", nameof(EncodingJobManager));
                throw;
            }

            Initialized = true;
            HelperMethods.DebugLog($"{nameof(EncodingJobManager)} Initialized", nameof(EncodingJobManager));
        }
    }

    public override void Start()
    {
        try
        {
            if (Initialized is false)
                throw new InvalidOperationException($"{nameof(EncodingJobManager)} is not initialized");

            StartManagerProcess();
            StartRequestHandler();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to start {nameof(EncodingJobManager)}", nameof(EncodingJobManager));
            throw;
        }

        Logger.LogInfo($"{nameof(EncodingJobManager)} Started", nameof(EncodingJobManager));
    }

    public override void Shutdown()
    {
        try
        {
            Requests.CompleteAdding();

            _encodingJobManagerMRE.Set();

            ShutdownCancellationTokenSource.Cancel();
            EncodingJobBuilderCancellationToken?.Cancel();
            EncodingCancellationToken?.Cancel();
            EncodingJobPostProcessingCancellationToken?.Cancel();

            EncodingJobBuilderTask?.Wait();
            EncodingTask?.Wait();
            EncodingJobPostProcessingTask?.Wait();

            ManagerProcessTask?.Wait();
            RequestHandlerTask?.Wait();

            Logger.LogInfo($"{nameof(EncodingJobManager)} Shutdown", nameof(EncodingJobManager));

            ShutdownMRE.Set();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to shutdown {nameof(EncodingJobManager)}", nameof(EncodingJobManager));
            throw;
        }
    }

    #endregion Initialize / Start / Shutdown

    #region Private Methods
    private void EncodingJobQueue_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            IEncodingJobModel addedEncodingJob = e.NewItems.Cast<IEncodingJobModel>().FirstOrDefault();
            if (addedEncodingJob is not null)
            {
                (string topic, CommunicationMessage<ClientUpdateType> message) = ClientUpdateMessageFactory.CreateEncodingJobQueueUpdate(EncodingJobQueueUpdateType.Add, addedEncodingJob.Id, addedEncodingJob.ToEncodingJobData());
                ClientUpdatePublisher.AddClientUpdateRequest(topic, message);
            }

            // If something was added, set MRE to make thread look to see if there is any processing to spin up
            _encodingJobManagerMRE.Set();
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            IEncodingJobModel removedEncodingJob = e.OldItems.Cast<IEncodingJobModel>().FirstOrDefault();
            if (removedEncodingJob is not null)
            {
                (string topic, CommunicationMessage<ClientUpdateType> message) = ClientUpdateMessageFactory.CreateEncodingJobQueueUpdate(EncodingJobQueueUpdateType.Remove, removedEncodingJob.Id, null);
                ClientUpdatePublisher.AddClientUpdateRequest(topic, message);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Move)
        {
            // TODO
        }
    }

    private void EncodingJob_EncodingJobStatusChanged(object sender, Models.Data.EncodingJobStatusChangedEventArgs e)
    {
        if (sender is IEncodingJobModel encodingJob)
        {
            _sourceFileManager.AddUpdateSourceFileEncodingStatusRequest(encodingJob.SourceFileGuid, e.Status);
        }
    }

    private bool ExistsByFileName(string filename)
    {
        lock (_lock)
        {
            return _encodingJobQueue.Any(x => x.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary> Gets first EncodingJobModel (not paused or in error) from list with the given status. </summary>
    /// <param name="status"><see cref="EncodingJobStatus"/></param>
    /// <returns><see cref="IEncodingJobModel"/></returns>
    private IEncodingJobModel GetNextEncodingJobWithStatus(EncodingJobStatus status)
    {
        lock (_lock)
        {
            return _encodingJobQueue.FirstOrDefault(x => x.Status.Equals(status) && (x.Paused is false) && (x.HasError is false) && (x.Canceled is false));
        }
    }

    /// <summary> Gets first EncodingJobModel (not paused or in error) from list that has finished encoding and needs post-processing </summary>
    /// <returns><see cref="IEncodingJobModel"/></returns>
    private IEncodingJobModel GetNextEncodingJobForPostProcessing()
    {
        lock (_lock)
        {
            return _encodingJobQueue.FirstOrDefault(x => x.Status.Equals(EncodingJobStatus.ENCODED) &&
                                        x.CompletedEncodingDateTime.HasValue &&
                                        x.NeedsPostProcessing &&
                                        (x.Paused is false) && (x.HasError is false));
        }
    }

    /// <summary>Gets encoding jobs that have been encoded and do not need post-processing. </summary>
    /// <returns>IReadOnlyList of <see cref="IEncodingJobModel>"/></returns>
    private IReadOnlyList<IEncodingJobModel> GetEncodedJobs()
    {
        lock (_lock)
        {
            return _encodingJobQueue.Where(x => x.Status.Equals(EncodingJobStatus.ENCODED) &&
                                            x.CompletedEncodingDateTime.HasValue &&
                                            x.NeedsPostProcessing is false).ToList();
        }
    }

    /// <summary>Gets encoding jobs that have been post-processed (and completed encoding). </summary>
    /// <returns>IReadOnlyList of <see cref="IEncodingJobModel>"/></returns>
    private IReadOnlyList<IEncodingJobModel> GetPostProcessedJobs()
    {
        lock (_lock)
        {
            return _encodingJobQueue.Where(x => x.Status.Equals(EncodingJobStatus.POST_PROCESSED) &&
                                            x.CompletedPostProcessingTime.HasValue).ToList();
        }
    }

    private IReadOnlyList<IEncodingJobModel> GetErroredJobs()
    {
        lock (_lock)
        {
            return _encodingJobQueue.Where(x => x.HasError).ToList();
        }
    }

    /*
    /// <summary>Moves encoding job with given id up one in the list.</summary>
    /// <param name="jobId">Id of job to move</param>
    private void MoveEncodingJobForward(ulong jobId)
    {
        int jobIndex = EncodingJobQueue.FindIndex(x => x.Id == jobId);
        // Already at the front of the list or not found
        if (jobIndex == 0 || jobIndex == -1) return;

        lock (_lock)
        {
            (EncodingJobQueue[jobIndex - 1], EncodingJobQueue[jobIndex]) = (EncodingJobQueue[jobIndex], EncodingJobQueue[jobIndex - 1]);
        }
    }
    /// <summary>Moves encoding job with given id back one in the list.</summary>
    /// <param name="jobId">Id of job to move</param>
    private void MoveEncodingJobBack(ulong jobId)
    {
        int jobIndex = EncodingJobQueue.FindIndex(x => x.Id == jobId);

        // Already at the back of the list or not found
        if (jobIndex == (EncodingJobQueue.Count - 1) || jobIndex == -1) return;

        lock (_lock)
        {
            (EncodingJobQueue[jobIndex + 1], EncodingJobQueue[jobIndex]) = (EncodingJobQueue[jobIndex], EncodingJobQueue[jobIndex + 1]);
        }
    }
    */
    #endregion Private Methods
}
