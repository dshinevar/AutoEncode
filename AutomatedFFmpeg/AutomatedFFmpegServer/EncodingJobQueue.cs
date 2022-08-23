using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomatedFFmpegServer
{
    public static class EncodingJobQueue
    {
        private static readonly List<EncodingJob> jobQueue = new();
        private static readonly object jobLock = new();
        private static int _idNumber = 1;
        private static int IdNumber 
        {
            get
            {
                int tmp = _idNumber;
                _idNumber++;
                return tmp;
            }
        }

        public static bool Any() => jobQueue.Any();

        /// <summary>Gets current list of encoding jobs. </summary>
        /// <returns>EncodingJob list</returns>
        public static List<EncodingJob> GetEncodingJobs()
        {
            lock (jobLock)
            {
                return jobQueue;
            }
        }

        public static List<EncodingJobClientData> GetEncodingJobsForClient()
        {
            lock (jobLock)
            {
                return jobQueue.Count > 0 ? jobQueue.ConvertAll(x => new EncodingJobClientData(x)).ToList() : new List<EncodingJobClientData>();
            }
        }

        /// <summary>Creates an EncodingJob and adds to queue based off the given info. </summary>
        /// <param name="videoSourceData"><see cref="VideoSourceData"/></param>
        /// <param name="postProcessingSettings"><see cref="PostProcessingSettings"/></param>
        /// <param name="sourceDirectoryPath">Directory path of source</param>
        /// <param name="destinationDirectoryPath">Directory path of destination</param>
        /// <param name="plexEnabled">Config override for PLEX post processing</param>
        /// <returns>The JobId of the newly created job; -1, otherwise.</returns>
        public static int CreateEncodingJob(VideoSourceData videoSourceData, PostProcessingSettings postProcessingSettings, string sourceDirectoryPath, string destinationDirectoryPath, bool plexEnabled)
        {
            int jobId = -1;
            if (!ExistsByFileName(videoSourceData.FileName))
            {
                EncodingJob newJob = new(IdNumber, videoSourceData.FullPath,
                                            videoSourceData.FullPath.Replace(sourceDirectoryPath, destinationDirectoryPath), 
                                            postProcessingSettings, plexEnabled);
                lock (jobLock)
                {
                    jobQueue.Add(newJob);
                    jobId = newJob.JobId;
                }
            }

            return jobId;
        }
        /// <summary>Removes an encoding job from the list.</summary>
        /// <param name="job">EncodingJob</param>
        public static bool RemoveEncodingJob(EncodingJob job)
        {
            lock (jobLock)
            {
                return jobQueue.Remove(job);
            }
        }

        /// <summary> Checks to see if a job exists by the given filename. </summary>
        /// <param name="filename"></param>
        /// <returns>True if a job exists with that filename; False, otherwise.</returns>
        public static bool ExistsByFileName(string filename)
        {
            lock (jobLock)
            {
                return jobQueue.Exists(x => x.FileName.Equals(filename));
            }
        }

        /// <summary> Checks to see if a job exists by the given id. </summary>
        /// <param name="id"></param>
        /// <returns>True if a job exists by that job id; False, otherwise.</returns>
        public static bool ExistsById(int id)
        {
            lock (jobLock)
            {
                return jobQueue.Exists(x => x.JobId == id);
            }
        }

        /// <summary> Gets first EncodingJob (not paused or in error) from list with the given status. </summary>
        /// <param name="status">EncodingJobStatus</param>
        /// <returns><see cref="EncodingJob"/></returns>
        public static EncodingJob GetNextEncodingJobWithStatus(EncodingJobStatus status)
        {
            lock (jobLock)
            {
                return jobQueue.Find(x => x.Status.Equals(status) && (x.Paused is false) && (x.Error is false));
            }
        }

        /// <summary> Gets first Encoding Job (not paused or in error) from list that has finished encoding and needs post-processing </summary>
        /// <returns><see cref="EncodingJob"/></returns>
        public static EncodingJob GetNextEncodingJobForPostProcessing()
        {
            lock (jobLock)
            {
                return jobQueue.Find(x => x.Status.Equals(EncodingJobStatus.ENCODED) &&
                                            x.CompletedEncodingDateTime.HasValue &&
                                            !x.PostProcessingFlags.Equals(PostProcessingFlags.None) &&
                                            (x.Paused is false) && (x.Error is false));
            }
        }

        /// <summary>Moves encoding job with given id up one in the list.</summary>
        /// <param name="jobId">Id of job to move</param>
        public static void MoveEncodingJobForward(int jobId)
        {
            int jobIndex = jobQueue.FindIndex(x => x.JobId == jobId);
            // Already at the front of the list or not found
            if (jobIndex == 0 || jobIndex == -1) return;

            lock (jobLock)
            {
                (jobQueue[jobIndex - 1], jobQueue[jobIndex]) = (jobQueue[jobIndex], jobQueue[jobIndex - 1]);
            }
        }
        /// <summary>Moves encoding job with given id back one in the list.</summary>
        /// <param name="jobId">Id of job to move</param>
        public static void MoveEncodingJobBack(int jobId)
        {
            int jobIndex = jobQueue.FindIndex(x => x.JobId == jobId);

            // Already at the back of the list or not found
            if (jobIndex == (jobQueue.Count - 1) || jobIndex == -1) return;

            lock (jobLock)
            {
                (jobQueue[jobIndex + 1], jobQueue[jobIndex]) = (jobQueue[jobIndex], jobQueue[jobIndex + 1]);
            }
        }

        /// <summary> Clears completed jobs </summary>
        /// <param name="hoursSinceCompleted">The number of hours a job needs to have been marked completed before removal.</param>
        /// <returns>A <see cref="IList{T}"/> of strings of the removed jobs.</returns>
        public static IList<string> ClearCompletedJobs(int hoursSinceCompleted)
        {
            IList<string> jobsRemoved = new List<string>();

            // Handle jobs that don't need post processing
            IEnumerable<EncodingJob> completedJobs = jobQueue.Where(x => x.Status >= EncodingJobStatus.ENCODED && 
                                                                    x.CompletedEncodingDateTime.HasValue && 
                                                                    x.PostProcessingFlags.Equals(PostProcessingFlags.None) is true).ToList();
            foreach (EncodingJob job in completedJobs)
            {
                // If it's been completed for longer than the given number of hours, remove job
                TimeSpan ts = DateTime.Now.Subtract((DateTime)job.CompletedEncodingDateTime);
                if (ts.TotalHours >= hoursSinceCompleted)
                {
                    bool success = RemoveEncodingJob(job);
                    if (success) jobsRemoved.Add(job.Name);
                }
            }

            // Handle jobs that need post processsing
            completedJobs = jobQueue.Where(x => x.Status.Equals(EncodingJobStatus.POST_PROCESSED) && x.CompletedPostProcessingTime.HasValue).ToList();
            foreach (EncodingJob job in completedJobs)
            {
                // If it's been completed for longer than the given number of hours, remove job
                TimeSpan ts = DateTime.Now.Subtract((DateTime)job.CompletedPostProcessingTime);
                if (ts.TotalHours >= hoursSinceCompleted)
                {
                    bool success = RemoveEncodingJob(job);
                    if (success) jobsRemoved.Add(job.Name);
                }
            }

            return jobsRemoved;
        }

        /// <summary> Clears errored jobs </summary>
        /// <param name="hoursSinceErrored">The number of hours a job needs to have been marked in error before removal.</param>
        /// <returns>A <see cref="IList{T}"/> of strings of the removed jobs.</returns>
        public static IList<string> ClearErroredJobs(int hoursSinceErrored)
        {
            IList<string> jobsRemoved = new List<string>();

            // Handle jobs that don't need post processing
            IEnumerable<EncodingJob> erroredJobs = jobQueue.Where(x => x.Error is true).ToList();

            foreach (EncodingJob job in erroredJobs)
            {
                TimeSpan ts = DateTime.Now.Subtract((DateTime)job.ErrorTime);
                if (ts.TotalHours >= hoursSinceErrored)
                {
                    bool success = RemoveEncodingJob(job);
                    if (success) jobsRemoved.Add(job.Name);
                }
            }

            return jobsRemoved;
        }

        public static string Output()
        { 
            string output = string.Empty;
            foreach (EncodingJob job in jobQueue)
            {
                output += $"{job.JobId} - {job.FileName} ";
            }
            return output;
        }
    }
}
