using AutomatedFFmpegUtilities.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomatedFFmpegServer
{
    public partial class AFServerMainThread
    {
        private static string MaintenanceThreadName => $"{ThreadName}-Maintenance";

        /// <summary>
        /// Runs maintenance tasks. Should be ran infrequently.
        /// </summary>
        /// <param name="obj"></param>
        private void OnMaintenanceTimerElapsed(object obj)
        {
            ClearCompletedJobs();
            ClearErroredJobs();
        }

        /// <summary>Clears out completed encoding jobs.</summary>
        private void ClearCompletedJobs()
        {
            List<string> jobsRemovedLog = new();

            // Encoded jobs that don't need post-processing
            IReadOnlyList<EncodingJob> encodedJobs = EncodingJobQueue.GetEncodedEncodingJobs();
            foreach (EncodingJob job in encodedJobs)
            {
                try
                {
                    // If it's been completed for longer than the given number of hours, remove job
                    TimeSpan ts = DateTime.Now.Subtract((DateTime)job.CompletedEncodingDateTime);
                    if (ts.TotalHours >= Config.GlobalJobSettings.HoursCompletedUntilRemoval)
                    {
                        bool success = EncodingJobQueue.RemoveEncodingJobById(job.Id);
                        if (success is true)
                        {
                            jobsRemovedLog.Add(job.ToString());
                        }
                        else
                        {
                            Logger.LogError($"Error removing completed {job} from queue.", MaintenanceThreadName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error removing completed {job} from queue.", MaintenanceThreadName);
                }
            }

            // Jobs that were post-processed
            IReadOnlyList<EncodingJob> postProcessedJobs = EncodingJobQueue.GetPostProcessedEncodingJobs();
            foreach (EncodingJob job in postProcessedJobs)
            {
                try
                {
                    // If it's been completed for longer than the given number of hours, remove job
                    TimeSpan ts = DateTime.Now.Subtract((DateTime)job.CompletedPostProcessingTime);
                    if (ts.TotalHours >= Config.GlobalJobSettings.HoursCompletedUntilRemoval)
                    {
                        bool success = EncodingJobQueue.RemoveEncodingJobById(job.Id);
                        if (success is true)
                        {
                            jobsRemovedLog.Add(job.ToString());
                        }
                        else
                        {
                            Logger.LogError($"Error removing post-processed {job} from queue.", MaintenanceThreadName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error removing post-processed {job} from queue.", MaintenanceThreadName);
                }
            }

            if (jobsRemovedLog.Any())
            {
                jobsRemovedLog.Insert(0, "Completed Jobs Removed");
                Logger.LogInfo(jobsRemovedLog, ThreadName);
            }
        }

        /// <summary>Clears out errored encoding jobs.</summary>
        private void ClearErroredJobs()
        {
            List<string> jobsRemovedLog = new();

            IReadOnlyList<EncodingJob> erroredJobs = EncodingJobQueue.GetErroredJobs();
            foreach (EncodingJob job in erroredJobs)
            {
                try
                {
                    // If it's been errored for longer than the given number of hours, remove job
                    TimeSpan ts = DateTime.Now.Subtract((DateTime)job.ErrorTime);
                    if (ts.TotalHours >= Config.GlobalJobSettings.HoursErroredUntilRemoval)
                    {
                        bool success = EncodingJobQueue.RemoveEncodingJobById(job.Id);
                        if (success is true)
                        {
                            jobsRemovedLog.Add(job.ToString());
                        }
                        else
                        {
                            Logger.LogError($"Error removing errored {job} from queue.", MaintenanceThreadName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error removing errored {job} from queue.", MaintenanceThreadName);
                }
            }

            if (jobsRemovedLog.Any())
            {
                jobsRemovedLog.Insert(0, "Errored Jobs Removed");
                Logger.LogInfo(jobsRemovedLog, MaintenanceThreadName);
            }
        }
    }
}
