using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomatedFFmpegServer
{
    public partial class AFServerMainThread
    {
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
            try
            {
                var jobsRemoved = EncodingJobQueue.ClearCompletedJobs(Config.GlobalJobSettings.HoursCompletedUntilRemoval);
                if (jobsRemoved?.Any() ?? false)
                {
                    List<string> jobsRemovedLog = new() { "Completed Jobs Removed" };
                    jobsRemovedLog.AddRange(jobsRemoved);
                    Logger.LogInfo(jobsRemovedLog, ThreadName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error removing completed jobs.", ThreadName);
            }
        }

        /// <summary>Clears out errored encoding jobs.</summary>
        private void ClearErroredJobs()
        {
            try
            {
                var jobsRemoved = EncodingJobQueue.ClearErroredJobs(Config.GlobalJobSettings.HoursErroredUntilRemoval);
                if (jobsRemoved?.Any() ?? false)
                {
                    List<string> jobsRemovedLog = new() { "Errored Jobs Removed" };
                    jobsRemovedLog.AddRange(jobsRemoved);
                    Logger.LogInfo(jobsRemovedLog, ThreadName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error removing errored jobs.", ThreadName);
            }
        }
    }
}
