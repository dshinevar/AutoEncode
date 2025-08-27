using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities.Enums;
using AutoEncodeServer.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

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

    private Timer JobRemovalTimer { get; set; }

    private readonly AsyncManualResetEvent _processMRE = new(false, true);

    protected async override void Process()
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

                        EncodingJobBuilderTask = Task.Run(async () =>
                        {
                            await jobToBuild.Build(EncodingJobBuilderCancellationToken);
                            jobToBuild.CleanupJob();
                            //_processMRE.Set();

                        }, EncodingJobBuilderCancellationToken.Token);

                        // throwaway task to set the MRE AFTER the original task is completed -- helps avoid race conditions
                        _ = EncodingJobBuilderTask.ContinueWith(t => _processMRE.Set());
                    }
                }

                if (EncodingTask?.IsCompletedSuccessfully ?? true)
                {
                    IEncodingJobModel jobToEncode = GetNextEncodingJobWithStatus(EncodingJobStatus.BUILT);
                    if (jobToEncode is not null)
                    {
                        EncodingCancellationToken = new CancellationTokenSource();

                        EncodingTask = Task.Run(() =>
                        {
                            jobToEncode.Encode(EncodingCancellationToken);
                            jobToEncode.CleanupJob();
                            //_processMRE.Set();

                        }, EncodingCancellationToken.Token);

                        // throwaway task to set the MRE AFTER the original task is completed -- helps avoid race conditions
                        _ = EncodingTask.ContinueWith(t => _processMRE.Set());
                    }
                }

                if (EncodingJobPostProcessingTask?.IsCompletedSuccessfully ?? true)
                {
                    IEncodingJobModel jobToPostProcess = GetNextEncodingJobForPostProcessing();
                    if (jobToPostProcess is not null)
                    {
                        EncodingJobPostProcessingCancellationToken = new CancellationTokenSource();

                        EncodingJobPostProcessingTask = Task.Run(() =>
                        {
                            jobToPostProcess.PostProcess(EncodingJobPostProcessingCancellationToken);
                            jobToPostProcess.CleanupJob();
                            //_processMRE.Set();

                        }, EncodingJobPostProcessingCancellationToken.Token);

                        // throwaway task to set the MRE AFTER the original task is completed -- helps avoid race conditions
                        _ = EncodingJobPostProcessingTask.ContinueWith(t => _processMRE.Set());
                    }
                }

                JobRemovalTimer ??= new((e) => ClearCompletedAndErroredJobs(), null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

                await _processMRE.WaitAsync(ShutdownCancellationTokenSource.Token);
                _processMRE.Reset();
            }
            else
            {
                JobRemovalTimer?.Dispose();
                JobRemovalTimer = null;
                await _processMRE.WaitAsync(ShutdownCancellationTokenSource.Token);   // Wait until signalled -- either for shutdown or job added to queue
            }
        }

        // Don't end main processing thread until other threads are done
        JobRemovalTimer?.Dispose();
        EncodingJobBuilderTask?.Wait();
        EncodingTask?.Wait();
        EncodingJobPostProcessingTask?.Wait();
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
