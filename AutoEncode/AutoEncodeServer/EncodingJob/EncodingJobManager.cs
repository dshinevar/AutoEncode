using AutoEncodeServer.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.EncodingJob;

public partial class EncodingJobManager : IEncodingJobManager
{
    #region Dependencies
    public ILogger Logger { get; set; }

    public IClientUpdatePublisher ClientUpdatePublisher { get; set; }
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

    private readonly ObservableCollection<IEncodingJobModel> EncodingJobQueue = [];
    private Timer _jobTaskTimer = null;
    private readonly ManualResetEvent _jobTaskShutdown = new(false);

    private AEServerConfig State { get; set; }
    private ManualResetEvent ShutdownMRE { get; set; }
    #endregion Private Properties

    #region Public Properties
    public int Count => EncodingJobQueue.Count;
    #endregion Public Properties

    /// <summary>Default Constructor</summary>
    public EncodingJobManager()
    {
        EncodingJobQueue.CollectionChanged += EncodingJobQueue_CollectionChanged;
    }

    #region Initialize / Start / Shutdown
    public void Initialize(AEServerConfig serverState, ManualResetEvent shutdownMRE)
    {
        if (_initialized is false)
        {
            try
            {
                State = serverState;
                ShutdownMRE = shutdownMRE;

                ClientUpdatePublisher.Initialize(State.ConnectionSettings.ClientUpdatePort);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to initialize {nameof(EncodingJobManager)}", nameof(EncodingJobManager));
                throw;
            }

            _initialized = true;
        }
    }

    public void Start()
    {
        if (_initialized is false) throw new Exception($"{nameof(EncodingJobManager)} is not initialized");

        try
        {
            _jobTaskTimer = new Timer(EncodingJobTaskHandler, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(5));

            ClientUpdatePublisher.Start();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to start {nameof(EncodingJobManager)}", nameof(EncodingJobManager));
            throw;
        }
    }

    public void Shutdown()
    {
        try
        {
            _jobTaskTimer?.Dispose(_jobTaskShutdown);
            _jobTaskShutdown.WaitOne(TimeSpan.FromSeconds(30));
            _jobTaskShutdown.Dispose();

            EncodingJobBuilderCancellationToken?.Cancel();
            EncodingCancellationToken?.Cancel();
            EncodingJobPostProcessingCancellationToken?.Cancel();

            EncodingJobBuilderTask?.Wait();
            EncodingTask?.Wait();
            EncodingJobPostProcessingTask?.Wait();

            ClientUpdatePublisher.Shutdown();

            ShutdownMRE.Set();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to shut down {nameof(EncodingJobManager)}", nameof(EncodingJobManager));
            throw;
        }
    }

    #endregion Initialize / Start / Shutdown

    #region Queue Methods
    public IEnumerable<EncodingJobData> GetEncodingJobQueue()
    {
        lock (_lock)
        {
            return EncodingJobQueue.Select(x => x.ToEncodingJobData()).ToList();
        }
    }

    public ulong? CreateEncodingJob(SourceFileData sourceFileData, PostProcessingSettings postProcessingSettings)
    {
        ulong? jobId = null;
        if (ExistsByFileName(sourceFileData.FileName) is false)
        {
            IEncodingJobModel model = new EncodingJobModel(IdNumber, sourceFileData.FullPath, sourceFileData.DestinationFullPath, postProcessingSettings);

            lock (_lock)
            {
                model.PropertyChanged += EncodingJobPropertyChanged;
                EncodingJobQueue.Add(model);
                jobId = model.Id;
            }
        }

        return jobId;
    }

    public bool IsEncodingByFileName(string filename)
    {
        lock (_lock)
        {
            return EncodingJobQueue.FirstOrDefault(x => x.FileName.Equals(filename))?.Status.Equals(EncodingJobStatus.ENCODING) ?? false;
        }
    }

    public bool RemoveEncodingJobById(ulong id)
    {
        bool success = false;

        lock (_lock)
        {
            IEncodingJobModel job = EncodingJobQueue.SingleOrDefault(x => x.Id == id);
            if (job is not null) success = EncodingJobQueue.Remove(job);
        }

        return success;
    }
    #endregion Queue Methods

    #region Job Methods
    public bool CancelJob(ulong jobId)
    {
        bool success = false;
        lock (_lock)
        {
            IEncodingJobModel job = EncodingJobQueue.FirstOrDefault(x => x.Id == jobId);

            if (job is not null)
            {
                job.Cancel();
                success = true;
            }
        }

        return success;
    }

    public bool PauseJob(ulong jobId)
    {
        bool success = false;
        lock (_lock)
        {
            IEncodingJobModel job = EncodingJobQueue.FirstOrDefault(x => x.Id == jobId);

            if (job is not null)
            {
                job.Pause();
                success = true;
            }
        }

        return success;
    }

    public bool ResumeJob(ulong jobId)
    {
        bool success = false;
        lock (_lock)
        {
            IEncodingJobModel job = EncodingJobQueue.FirstOrDefault(x => x.Id == jobId);

            if (job is not null)
            {
                job.Resume();
                success = true;
            }
        }

        return success;
    }

    public bool CancelThenPauseJob(ulong jobId)
    {
        bool success = false;
        lock (_lock)
        {
            IEncodingJobModel job = EncodingJobQueue.FirstOrDefault(x => x.Id == jobId);

            if (job is not null)
            {
                job.Cancel();
                job.Pause();
                success = true;
            }
        }

        return success;
    }
    #endregion Job Methods

    #region Private Methods

    private void EncodingJobQueue_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Task.Run(() =>
        {
            try
            {
                IEnumerable<EncodingJobData> queue = GetEncodingJobQueue();
                ClientUpdatePublisher.SendUpdateToClients(CommunicationConstants.EncodingJobQueueUpdate, new EncodingJobQueueUpdateData(queue));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to send encoding job queue update to clients.", nameof(EncodingJobManager));
            }
        });
    }

    private void EncodingJobPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        if (sender is IEncodingJobModel model)
        {
            Task.Run(() =>
            {
                try
                {
                    switch (args.PropertyName)
                    {
                        case CommunicationConstants.EncodingJobStatusUpdate:
                        {
                            EncodingJobStatusUpdateData updateData = model.GetStatusUpdate();
                            ClientUpdatePublisher.SendUpdateToClients($"{CommunicationConstants.EncodingJobStatusUpdate}-{model.Id}", updateData);
                            break;
                        }
                        case CommunicationConstants.EncodingJobEncodingProgressUpdate:
                        {
                            EncodingJobEncodingProgressUpdateData updateData = model.GetEncodingUpdate();
                            ClientUpdatePublisher.SendUpdateToClients($"{CommunicationConstants.EncodingJobEncodingProgressUpdate}-{model.Id}", updateData);
                            break;
                        }
                        case CommunicationConstants.EncodingJobProcessingDataUpdate:
                        {
                            EncodingJobProcessingDataUpdateData updateData = model.GetProcessingDataUpdate();
                            ClientUpdatePublisher.SendUpdateToClients($"{CommunicationConstants.EncodingJobProcessingDataUpdate}-{model.Id}", updateData);
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Failed to send encoding job update to clients.", nameof(EncodingJobManager), new { PropertyGroup = args.PropertyName });
                }
            });
        }
    }

    private bool ExistsByFileName(string filename)
    {
        lock (_lock)
        {
            return EncodingJobQueue.Any(x => x.FileName.Equals(filename, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary> Gets first EncodingJobModel (not paused or in error) from list with the given status. </summary>
    /// <param name="status"><see cref="EncodingJobStatus"/></param>
    /// <returns><see cref="IEncodingJobModel"/></returns>
    private IEncodingJobModel GetNextEncodingJobWithStatus(EncodingJobStatus status)
    {
        lock (_lock)
        {
            return EncodingJobQueue.FirstOrDefault(x => x.Status.Equals(status) && (x.Paused is false) && (x.HasError is false) && (x.Canceled is false));
        }
    }

    /// <summary> Gets first EncodingJobModel (not paused or in error) from list that has finished encoding and needs post-processing </summary>
    /// <returns><see cref="IEncodingJobModel"/></returns>
    private IEncodingJobModel GetNextEncodingJobForPostProcessing()
    {
        lock (_lock)
        {
            return EncodingJobQueue.FirstOrDefault(x => x.Status.Equals(EncodingJobStatus.ENCODED) &&
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
            return EncodingJobQueue.Where(x => x.Status.Equals(EncodingJobStatus.ENCODED) &&
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
            return EncodingJobQueue.Where(x => x.Status.Equals(EncodingJobStatus.POST_PROCESSED) &&
                                            x.CompletedPostProcessingTime.HasValue).ToList();
        }
    }

    private IReadOnlyList<IEncodingJobModel> GetErroredJobs()
    {
        lock (_lock)
        {
            return EncodingJobQueue.Where(x => x.HasError).ToList();
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
