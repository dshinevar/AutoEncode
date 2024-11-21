using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities.Enums;
using AutoEncodeServer.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.Managers;

// TASK HANDLER
public partial class EncodingJobManager : IEncodingJobManager
{
    private Task EncodingJobBuilderTask { get; set; }
    private CancellationTokenSource EncodingJobBuilderCancellationToken { get; set; }

    private Task EncodingTask { get; set; }
    private CancellationTokenSource EncodingCancellationToken { get; set; }

    private Task EncodingJobPostProcessingTask { get; set; }
    private CancellationTokenSource EncodingJobPostProcessingCancellationToken { get; set; }

    private readonly ManualResetEvent _encodingJobManagerMRE = new(false);

    protected override void Process()
    {
        while (ShutdownCancellationTokenSource.IsCancellationRequested is false)
        {
            if (_encodingJobQueue.Count > 0)
            {
                // Check if task is done (or null -- first time setup)
                if (EncodingJobBuilderTask?.IsCompletedSuccessfully ?? true)
                {
                    IEncodingJobModel jobToBuild = GetNextEncodingJobWithStatus(EncodingJobStatus.NEW);
                    if (jobToBuild is not null)
                    {
                        EncodingJobBuilderCancellationToken = new CancellationTokenSource();

                        EncodingJobBuilderTask = Task.Run(() => jobToBuild.Build(EncodingJobBuilderCancellationToken), EncodingJobBuilderCancellationToken.Token)
                                                        .ContinueWith(t =>
                                                        {
                                                            jobToBuild.CleanupJob();
                                                            _encodingJobManagerMRE.Set();
                                                        });
                    }
                }

                if (EncodingTask?.IsCompletedSuccessfully ?? true)
                {
                    IEncodingJobModel jobToEncode = GetNextEncodingJobWithStatus(EncodingJobStatus.BUILT);
                    if (jobToEncode is not null)
                    {
                        EncodingCancellationToken = new CancellationTokenSource();

                        EncodingTask = Task.Run(() => jobToEncode.Encode(EncodingCancellationToken), EncodingCancellationToken.Token)
                                            .ContinueWith(t =>
                                            {
                                                jobToEncode.CleanupJob();
                                                _encodingJobManagerMRE.Set();
                                            });
                    }
                }

                if (EncodingJobPostProcessingTask?.IsCompletedSuccessfully ?? true)
                {
                    IEncodingJobModel jobToPostProcess = GetNextEncodingJobForPostProcessing();
                    if (jobToPostProcess is not null)
                    {
                        EncodingJobPostProcessingCancellationToken = new CancellationTokenSource();

                        EncodingJobPostProcessingTask = Task.Run(() => jobToPostProcess.PostProcess(EncodingJobPostProcessingCancellationToken), EncodingJobPostProcessingCancellationToken.Token)
                                                            .ContinueWith(t =>
                                                            {
                                                                jobToPostProcess.CleanupJob();
                                                                _encodingJobManagerMRE.Set();
                                                            });
                    }
                }

                do
                {
                    ClearCompletedAndErroredJobs();
                }
                while (_encodingJobManagerMRE.WaitOne(TimeSpan.FromHours(1)) is false);

                _encodingJobManagerMRE.Reset();
            }
            else
            {
                _encodingJobManagerMRE.WaitOne();   // Wait until signalled -- either for shutdown or job added to queue
            }
        }
    }

    /// <summary>Adds jobs to request processing queue for removal.</summary>
    private void ClearCompletedAndErroredJobs()
    {
        // Encoded jobs that don't need post-processing
        IReadOnlyList<IEncodingJobModel> encodedJobs = GetEncodedJobs();
        foreach (IEncodingJobModel job in encodedJobs)
        {
            try
            {
                // If it's been completed for longer than the given number of hours, remove job
                TimeSpan ts = DateTime.Now.Subtract(job.CompletedEncodingDateTime.Value);
                if (ts.TotalHours >= State.HoursCompletedUntilRemoval)
                {
                    AddRemoveEncodingJobByIdRequest(job.Id, RemovedEncodingJobReason.Completed);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error adding completed job [{job}] for removal.", nameof(EncodingJobManager), new { job.Id, job.Name });
            }
        }

        // Jobs that were post-processed
        IReadOnlyList<IEncodingJobModel> postProcessedJobs = GetPostProcessedJobs();
        foreach (IEncodingJobModel job in postProcessedJobs)
        {
            try
            {
                // If it's been completed for longer than the given number of hours, remove job
                TimeSpan ts = DateTime.Now.Subtract((DateTime)job.CompletedPostProcessingTime);
                if (ts.TotalHours >= State.HoursCompletedUntilRemoval)
                {
                    AddRemoveEncodingJobByIdRequest(job.Id, RemovedEncodingJobReason.Completed);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error adding completed job [{job}] for removal.", nameof(EncodingJobManager), new { job.Id, job.Name });
            }
        }

        IReadOnlyList<IEncodingJobModel> erroredJobs = GetErroredJobs();
        foreach (IEncodingJobModel job in erroredJobs)
        {
            try
            {
                // If it's been errored for longer than the given number of hours, remove job
                TimeSpan ts = DateTime.Now.Subtract((DateTime)job.ErrorTime);
                if (ts.TotalHours >= State.HoursErroredUntilRemoval)
                {
                    AddRemoveEncodingJobByIdRequest(job.Id, RemovedEncodingJobReason.Errored);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error adding errored job [{job}] for removal.", nameof(EncodingJobManager), new { job.Id, job.Name });
            }
        }
    }
}
