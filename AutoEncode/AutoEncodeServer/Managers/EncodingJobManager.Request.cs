using AutoEncodeServer.Data.Request;
using AutoEncodeServer.Enums;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoEncodeServer.Managers;

// REQUEST
public partial class EncodingJobManager : IEncodingJobManager
{
    #region CreateEncodingJob Processing
    private const int _newEncodingJobRequestHandlerTimeout = 300000;   // 5 minutes
    private Task _newEncodingJobRequestHandlerTask;
    private readonly BlockingCollection<ISourceFileModel> _newEncodingJobRequests = [];

    private Task StartNewEncodingJobRequestHandler()
        => _newEncodingJobRequestHandlerTask = Task.Run(() =>
        {
            while (_newEncodingJobRequests.TryTake(out ISourceFileModel sourceFile, _newEncodingJobRequestHandlerTimeout, ShutdownCancellationTokenSource.Token))
            {
                CreateEncodingJob(sourceFile);
            }
        }, ShutdownCancellationTokenSource.Token);

    public bool AddCreateEncodingJobRequest(ISourceFileModel sourceFile)
    {
        if ((_newEncodingJobRequestHandlerTask is null) ||
            (_newEncodingJobRequestHandlerTask.Status != TaskStatus.Running) ||
            (_newEncodingJobRequestHandlerTask.IsCompleted is true))
        {
            StartNewEncodingJobRequestHandler();
        }

        return _newEncodingJobRequests.TryAdd(sourceFile);
    }

    private void CreateEncodingJob(ISourceFileModel sourceFile)
    {
        try
        {
            if (Count < State.MaxNumberOfJobsInQueue)
            {
                if ((ExistsByFileName(sourceFile.Filename) is false) && (HelperMethods.IsFileSizeChanging(sourceFile.FullPath) is true))
                {
                    PostProcessingSettings postProcessingSettings = State.Directories[sourceFile.SearchDirectoryName].PostProcessing;
                    // Prep Data for creating job
                    List<string> updatedCopyFilePaths = null;
                    if ((postProcessingSettings?.CopyFilePaths?.Count ?? -1) > 0 is true)
                    {
                        // Update copy file paths with full destination directory (for extras and shows with subdirectories)
                        updatedCopyFilePaths = [];
                        foreach (string oldPath in postProcessingSettings.CopyFilePaths)
                        {
                            updatedCopyFilePaths.Add($"{oldPath}{sourceFile.FullPath.Replace(sourceFile.SourceDirectory, "")}");
                        }
                    }

                    PostProcessingSettings updatedPostProcessingSettings = new()
                    {
                        CopyFilePaths = updatedCopyFilePaths,
                        DeleteSourceFile = postProcessingSettings?.DeleteSourceFile ?? false
                    };

                    IEncodingJobModel newEncodingJob = EncodingJobModelFactory.Create(IdNumber, sourceFile.Guid, sourceFile.FullPath, sourceFile.DestinationFullPath, updatedPostProcessingSettings);
                    newEncodingJob.EncodingJobStatusChanged += EncodingJob_EncodingJobStatusChanged;

                    lock (_lock)
                    {
                        _encodingJobQueue.Add(newEncodingJob);
                    }

                    Logger.LogInfo($"{newEncodingJob} added to encoding job queue.", nameof(EncodingJobManager));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to create encoding job for {sourceFile.FullPath}", nameof(EncodingJobManager));
        }
    }
    #endregion CreateEncodingJob Processing


    #region Request Processing
    protected override void ProcessManagerRequest(ManagerRequest request)
    {
        try
        {
            switch (request.Type)
            {
                case ManagerRequestType.RemoveEncodingJobById:
                {
                    if (request is ManagerRequest<RemoveEncodingJobRequest> removeEncodingJobRequest)
                    {
                        RemoveEncodingJobById(removeEncodingJobRequest.RequestData.JobId, removeEncodingJobRequest.RequestData.Reason);
                    }
                    break;
                }
                case ManagerRequestType.CancelJobById:
                {
                    if (request is ManagerRequest<ulong> canceljobRequest)
                    {
                        CancelJobById(canceljobRequest.RequestData);
                    }
                    break;
                }
                case ManagerRequestType.PauseJobById:
                {
                    if (request is ManagerRequest<ulong> pausejobRequest)
                    {
                        PauseJobById(pausejobRequest.RequestData);
                    }
                    break;
                }
                case ManagerRequestType.ResumeJobById:
                {
                    if (request is ManagerRequest<ulong> resumejobRequest)
                    {
                        ResumeJobById(resumejobRequest.RequestData);
                        _encodingJobManagerMRE.Set();   // Force the process thread to keep running if resumed.
                    }
                    break;
                }
                case ManagerRequestType.PauseAndCancelJobById:
                {
                    if (request is ManagerRequest<ulong> pauseCanceljobRequest)
                    {
                        PauseAndCancelJobById(pauseCanceljobRequest.RequestData);
                    }
                    break;
                }
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Error processing request {request.Type}.", nameof(EncodingJobManager));
        }
    }

    private void RemoveEncodingJobById(ulong id, RemovedEncodingJobReason reason)
    {
        lock (_lock)
        {
            IEncodingJobModel job = _encodingJobQueue.SingleOrDefault(x => x.Id == id);
            if (job is not null)
            {
                if (job.IsProcessing)
                {
                    job.Pause();
                    job.Cancel();
                }

                if (_encodingJobQueue.Remove(job) is true)
                {
                    string msg;
                    switch (reason)
                    {
                        case RemovedEncodingJobReason.Completed:
                        {
                            msg = $"Completed Job [{job}] was removed from queue.";
                            break;
                        }
                        case RemovedEncodingJobReason.Errored:
                        {
                            msg = $"Errored Job [{job}] was removed from queue.";
                            break;
                        }
                        case RemovedEncodingJobReason.UserRequested:
                        {
                            msg = $"User Requested Job [{job}] was removed from queue.";
                            break;
                        }
                        case RemovedEncodingJobReason.None:
                        default:
                        {
                            msg = $"{job} was removed from queue.";
                            break;
                        }
                    }

                    Logger.LogInfo(msg, nameof(EncodingJobManager));
                }
            }
        }
    }

    private void CancelJobById(ulong id)
    {
        lock (_lock)
        {
            IEncodingJobModel job = _encodingJobQueue.FirstOrDefault(x => x.Id == id);

            job?.Cancel();
        }
    }

    private void PauseJobById(ulong id)
    {
        lock (_lock)
        {
            IEncodingJobModel job = _encodingJobQueue.FirstOrDefault(x => x.Id == id);

            job?.Pause();
        }
    }

    private void ResumeJobById(ulong id)
    {
        lock (_lock)
        {
            IEncodingJobModel job = _encodingJobQueue.FirstOrDefault(x => x.Id == id);

            job?.Resume();
        }
    }

    private void PauseAndCancelJobById(ulong id)
    {
        lock (_lock)
        {
            IEncodingJobModel job = _encodingJobQueue.FirstOrDefault(x => x.Id == id);

            job?.Pause();
            job?.Cancel();
        }
    }

    #endregion Request Processing


    #region Get Requests
    // NOTE: These are simple getters or lookups that don't require to be put on the processing request queue

    public IEnumerable<EncodingJobData> GetEncodingJobQueue()
    {
        lock (_lock)
        {
            return _encodingJobQueue.Select(x => x.ToEncodingJobData()).ToList();
        }
    }

    public bool IsEncodingByFileName(string filename)
    {
        lock (_lock)
        {
            return _encodingJobQueue.FirstOrDefault(x => x.Filename.Equals(filename))?.Status.Equals(EncodingJobStatus.ENCODING) ?? false;
        }
    }

    public EncodingJobStatus? GetEncodingJobStatusByFileName(string filename)
    {
        lock (_lock)
        {
            return _encodingJobQueue.FirstOrDefault(x => x.Filename.Equals(filename))?.Status;
        }
    }
    #endregion Get Requests


    #region Add Requests
    public bool AddRemoveEncodingJobByIdRequest(ulong id, RemovedEncodingJobReason reason)
        => TryAddRequest(new ManagerRequest<RemoveEncodingJobRequest>()
        {
            Type = ManagerRequestType.RemoveEncodingJobById,
            RequestData = new()
            {
                JobId = id,
                Reason = reason
            }
        });

    public bool AddCancelJobByIdRequest(ulong id)
        => TryAddRequest(new ManagerRequest<ulong>()
        {
            Type = ManagerRequestType.CancelJobById,
            RequestData = id
        });

    public bool AddPauseJobByIdRequest(ulong id)
        => TryAddRequest(new ManagerRequest<ulong>()
        {
            Type = ManagerRequestType.PauseJobById,
            RequestData = id
        });

    public bool AddResumeJobByIdRequest(ulong id)
        => TryAddRequest(new ManagerRequest<ulong>()
        {
            Type = ManagerRequestType.ResumeJobById,
            RequestData = id
        });

    public bool AddPauseAndCancelJobByIdRequest(ulong id)
        => TryAddRequest(new ManagerRequest<ulong>()
        {
            Type = ManagerRequestType.PauseAndCancelJobById,
            RequestData = id
        });
    #endregion Add Requests
}
