using AutoEncodeServer.Data;
using AutoEncodeServer.Enums;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoEncodeServer.Managers;

// REQUEST
public partial class EncodingJobManager : IEncodingJobManager
{
    #region CreateEncodingJob Processing
    private void CreateEncodingJob(ISourceFileModel sourceFile)
    {
        try
        {
            if (Count < State.MaxNumberOfJobsInQueue)
            {
                if ((ExistsBySourceFileGuid(sourceFile.Guid) is false) && (IsFileReady(sourceFile.FullPath) is true))
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

                    SourceFileManagerConnection.UpdateSourceFileEncodingStatus(sourceFile.Guid, newEncodingJob.Status);

                    Logger.LogInfo($"{newEncodingJob} added to encoding job queue.", nameof(EncodingJobManager));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to create encoding job for {sourceFile.FullPath}", nameof(EncodingJobManager));
        }
    }

    /// <summary>Check if file ready. Attempts to open a stream for the file.</summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>True if file is ready; False, otherwise</returns>
    private static bool IsFileReady(string filePath)
    {
        const int attempts = 5;

        FileInfo fileInfo = new(filePath);
        bool ready = true;
    
        for (int i = 0; i < attempts; i++)
        {
            try
            {
                using FileStream stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException) 
            {
                ready = false;
                break;
            }
        }

        return ready;
    }
    #endregion CreateEncodingJob Processing


    #region Request Processing
    private void RemoveEncodingJobById(ulong id, RemovedEncodingJobReason reason)
    {
        IEncodingJobModel job = null;
        try
        {
            lock (_lock)
            {
                job = _encodingJobQueue.SingleOrDefault(x => x.Id == id);
                if (job is not null)
                {
                    if (job.IsProcessing)
                    {
                        job.Pause();
                        job.Cancel();
                    }

                    if (_encodingJobQueue.Remove(job) is true)
                    {
                        Logger.LogInfo($"Job {job} was removed from queue. [Reason: {reason.GetDescription()}]", nameof(EncodingJobManager));
                        SourceFileManagerConnection.UpdateSourceFileEncodingStatus(job.SourceFileGuid, null);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Error removing encoding job with ID: {id}", nameof(EncodingJobManager), new { id, reason, job?.Filename });
        }
    }

    private void CancelJobById(ulong id)
    {
        IEncodingJobModel job = null;
        try
        {
            lock (_lock)
            {
                job = _encodingJobQueue.FirstOrDefault(x => x.Id == id);
                job?.Cancel();
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Error cancelling encoding job with ID: {id}", nameof(EncodingJobManager), new { id, job?.Filename });
        }
    }

    private void PauseJobById(ulong id)
    {
        IEncodingJobModel job = null;
        try
        {
            lock (_lock)
            {
                job = _encodingJobQueue.FirstOrDefault(x => x.Id == id);
                job?.Pause();
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Error pausing encoding job with ID: {id}", nameof(EncodingJobManager), new { id, job?.Filename });
        }
    }

    private void ResumeJobById(ulong id)
    {
        IEncodingJobModel job = null;
        try
        {
            lock (_lock)
            {
                job = _encodingJobQueue.FirstOrDefault(x => x.Id == id);
                job?.Resume();

                _encodingJobManagerMRE.Set();
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Error resuming encoding job with ID: {id}", nameof(EncodingJobManager), new { id, job?.Filename });
        }
    }

    private void PauseAndCancelJobById(ulong id)
    {
        IEncodingJobModel job = null;
        try
        {
            lock (_lock)
            {
                job = _encodingJobQueue.FirstOrDefault(x => x.Id == id);
                job?.Pause();
                job?.Cancel();
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Error pausing and cancelling encoding job with ID: {id}", nameof(EncodingJobManager), new { id, job?.Filename });
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

    public EncodingJobStatus? GetEncodingJobStatusBySourceFileGuid(Guid sourceFileGuid)
    {
        lock (_lock)
        {
            return _encodingJobQueue.FirstOrDefault(ej => ej.SourceFileGuid == sourceFileGuid)?.Status;
        }
    }
    #endregion Get Requests


    #region Add Requests
    public bool AddCreateEncodingJobRequest(ISourceFileModel sourceFile)
        => Requests.TryAdd(() => CreateEncodingJob(sourceFile));

    public bool AddRemoveEncodingJobByIdRequest(ulong id, RemovedEncodingJobReason reason)
        => Requests.TryAdd(() => RemoveEncodingJobById(id, reason));

    public bool AddCancelJobByIdRequest(ulong id)
        => Requests.TryAdd(() => CancelJobById(id));

    public bool AddPauseJobByIdRequest(ulong id)
        => Requests.TryAdd(() => PauseJobById(id));

    public bool AddResumeJobByIdRequest(ulong id)
        => Requests.TryAdd(() => ResumeJobById(id));

    public bool AddPauseAndCancelJobByIdRequest(ulong id)
        => Requests.TryAdd(() => PauseAndCancelJobById(id));
    #endregion Add Requests
}
