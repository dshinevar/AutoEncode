using AutoEncodeServer.Interfaces;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.EncodingJob
{
    public partial class EncodingJobManager : IEncodingJobManager
    {
        private Task EncodingJobBuilderTask { get; set; }
        private CancellationTokenSource EncodingJobBuilderCancellationToken { get; set; }

        private Task EncodingTask { get; set; }
        private CancellationTokenSource EncodingCancellationToken { get; set; }

        private Task EncodingJobPostProcessingTask { get; set; }
        private CancellationTokenSource EncodingJobPostProcessingCancellationToken { get; set; }

        private void EncodingJobTaskHandler(object obj)
        {
            if (EncodingJobQueue.Count > 0)
            {
                // Check if task is done (or null -- first time setup)
                if (EncodingJobBuilderTask?.IsCompletedSuccessfully ?? true)
                {
                    IEncodingJobModel jobToBuild = GetNextEncodingJobWithStatus(EncodingJobStatus.NEW);
                    if (jobToBuild is not null)
                    {
                        EncodingJobBuilderCancellationToken = new CancellationTokenSource();
                        jobToBuild.SetTaskCancellationToken(EncodingJobBuilderCancellationToken);

                        EncodingJobBuilderTask = Task.Run(()
                            => Build(jobToBuild, State.ServerSettings.FFmpegDirectory, State.ServerSettings.HDR10PlusExtractorFullPath,
                                                     State.ServerSettings.DolbyVisionExtractorFullPath, State.ServerSettings.X265FullPath,
                                                        State.GlobalJobSettings.DolbyVisionEncodingEnabled,
                                                            EncodingJobBuilderCancellationToken.Token), EncodingJobBuilderCancellationToken.Token)
                                                        .ContinueWith(t => CleanupJob(jobToBuild));
                    }
                }


                // Check if task is done (or null -- first time setup)
                if (EncodingTask?.IsCompletedSuccessfully ?? true)
                {
                    IEncodingJobModel jobToEncode = GetNextEncodingJobWithStatus(EncodingJobStatus.BUILT);
                    if (jobToEncode is not null)
                    {
                        EncodingCancellationToken = new CancellationTokenSource();
                        jobToEncode.SetTaskCancellationToken(EncodingCancellationToken);

                        if (State.GlobalJobSettings.DolbyVisionEncodingEnabled is true && jobToEncode.EncodingInstructions.VideoStreamEncodingInstructions.HasDolbyVision is true)
                        {
                            EncodingTask = Task.Run(()
                                => EncodeWithDolbyVision(jobToEncode, State.ServerSettings.FFmpegDirectory, State.ServerSettings.MkvMergeFullPath,
                                                                                EncodingCancellationToken.Token), EncodingCancellationToken.Token)
                                                            .ContinueWith(t => CleanupJob(jobToEncode));
                        }
                        else
                        {
                            EncodingTask = Task.Run(()
                                => Encode(jobToEncode, State.ServerSettings.FFmpegDirectory, EncodingCancellationToken.Token), EncodingCancellationToken.Token)
                                                            .ContinueWith(t => CleanupJob(jobToEncode));
                        }
                    }
                }



                if (EncodingJobPostProcessingTask?.IsCompletedSuccessfully ?? true)
                {
                    IEncodingJobModel jobToPostProcess = GetNextEncodingJobForPostProcessing();
                    if (jobToPostProcess is not null)
                    {
                        EncodingJobPostProcessingCancellationToken = new CancellationTokenSource();
                        jobToPostProcess.SetTaskCancellationToken(EncodingJobPostProcessingCancellationToken);

                        EncodingJobPostProcessingTask = Task.Factory.StartNew(()
                            => PostProcess(jobToPostProcess, EncodingJobPostProcessingCancellationToken.Token), EncodingJobPostProcessingCancellationToken.Token)
                                                        .ContinueWith(t => CleanupJob(jobToPostProcess));
                    }
                }

                ClearCompletedJobs();
                ClearErroredJobs();
            }
        }

        private static void CleanupJob(IEncodingJobModel job)
        {
            if (job.Canceled is true)
            {
                job.ResetCancel();
            }

            // If complete, no point in pausing, just "resume"
            if (job.Complete is false)
            {
                if (job.ToBePaused is true)
                {
                    job.Pause();
                }
            }
            else
            {
                job.Resume();
            }
        }

        /// <summary>Clears out completed encoding jobs.</summary>
        private void ClearCompletedJobs()
        {
            List<string> jobsRemovedLog = [];

            // Encoded jobs that don't need post-processing
            IReadOnlyList<IEncodingJobModel> encodedJobs = GetEncodedJobs();
            foreach (IEncodingJobModel job in encodedJobs)
            {
                try
                {
                    // If it's been completed for longer than the given number of hours, remove job
                    TimeSpan ts = DateTime.Now.Subtract((DateTime)job.CompletedEncodingDateTime);
                    if (ts.TotalHours >= State.GlobalJobSettings.HoursCompletedUntilRemoval)
                    {
                        bool success = RemoveEncodingJobById((ulong)job.Id);
                        if (success is true)
                        {
                            jobsRemovedLog.Add(job.ToString());
                        }
                        else
                        {
                            Logger.LogError($"Error removing completed {job} from queue.", nameof(EncodingJobManager));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error removing completed {job} from queue.", nameof(EncodingJobManager), new { job.Id, job.Name });
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
                    if (ts.TotalHours >= State.GlobalJobSettings.HoursCompletedUntilRemoval)
                    {
                        bool success = RemoveEncodingJobById((ulong)job.Id);
                        if (success is true)
                        {
                            jobsRemovedLog.Add(job.ToString());
                        }
                        else
                        {
                            Logger.LogError($"Error removing post-processed {job} from queue.", nameof(EncodingJobManager));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error removing post-processed {job} from queue.", nameof(EncodingJobManager), new { job.Id, job.Name });
                }
            }

            if (jobsRemovedLog.Count != 0)
            {
                jobsRemovedLog.Insert(0, "Completed Jobs Removed");
                Logger.LogInfo(jobsRemovedLog, nameof(EncodingJobManager));
            }
        }

        /// <summary>Clears out errored encoding jobs.</summary>
        private void ClearErroredJobs()
        {
            List<string> jobsRemovedLog = [];

            IReadOnlyList<IEncodingJobModel> erroredJobs = GetErroredJobs();
            foreach (IEncodingJobModel job in erroredJobs)
            {
                try
                {
                    // If it's been errored for longer than the given number of hours, remove job
                    TimeSpan ts = DateTime.Now.Subtract((DateTime)job.ErrorTime);
                    if (ts.TotalHours >= State.GlobalJobSettings.HoursErroredUntilRemoval)
                    {
                        bool success = RemoveEncodingJobById((ulong)job.Id);
                        if (success is true)
                        {
                            jobsRemovedLog.Add(job.ToString());
                        }
                        else
                        {
                            Logger.LogError($"Error removing errored {job} from queue.", nameof(EncodingJobManager));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error removing errored {job} from queue.", nameof(EncodingJobManager), new { job.Id, job.Name });
                }
            }

            if (jobsRemovedLog.Count != 0)
            {
                jobsRemovedLog.Insert(0, "Errored Jobs Removed");
                Logger.LogInfo(jobsRemovedLog, nameof(EncodingJobManager));
            }
        }
    }
}
