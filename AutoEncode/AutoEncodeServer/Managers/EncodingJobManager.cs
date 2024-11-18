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
using System.Threading.Tasks;

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
    private readonly object _lock = new();
    private bool _initialized = false;
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

    private Task _encodingJobManagerTask = null;
    private ManualResetEvent _shutdownMRE;

    #endregion Private Properties

    #region Public Properties
    public int Count => _encodingJobQueue.Count;
    #endregion Public Properties

    /// <summary>Default Constructor</summary>
    public EncodingJobManager()
    {
        _encodingJobQueue.CollectionChanged += EncodingJobQueue_CollectionChanged;
    }

    #region Initialize / Start / Shutdown
    public void Initialize(ManualResetEvent shutdownMRE)
    {
        if (_initialized is false)
        {
            try
            {
                _shutdownMRE = shutdownMRE;
                _shutdownMRE.Reset();

                _sourceFileManager = Container.Resolve<ISourceFileManager>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to initialize {nameof(EncodingJobManager)}", nameof(EncodingJobManager));
                throw;
            }

            _initialized = true;
            HelperMethods.DebugLog($"{nameof(EncodingJobManager)} Initialized", nameof(EncodingJobManager));
        }
    }

    public void Start()
    {
        try
        {
            if (_initialized is false)
                throw new InvalidOperationException($"{nameof(EncodingJobManager)} is not initialized");

            _encodingJobManagerTask = StartEncodingJobManagerThread();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to start {nameof(EncodingJobManager)}", nameof(EncodingJobManager));
            throw;
        }

        Logger.LogInfo($"{nameof(EncodingJobManager)} Started", nameof(EncodingJobManager));
    }

    public void Stop()
    {
        try
        {
            _encodingJobManagerMRE.Set();

            ShutdownCancellationTokenSource.Cancel();
            EncodingJobBuilderCancellationToken?.Cancel();
            EncodingCancellationToken?.Cancel();
            EncodingJobPostProcessingCancellationToken?.Cancel();

            EncodingJobBuilderTask?.Wait();
            EncodingTask?.Wait();
            EncodingJobPostProcessingTask?.Wait();

            _encodingJobManagerTask?.Wait();
            RequestHandlerTask?.Wait();

            Logger.LogInfo($"{nameof(EncodingJobManager)} Stopped", nameof(EncodingJobManager));

            _shutdownMRE.Set();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to shut down {nameof(EncodingJobManager)}", nameof(EncodingJobManager));
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
