﻿using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System.Collections.Generic;
using System.Linq;

namespace AutoEncodeServer
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

        public static int Count => jobQueue.Count;

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
        /// <param name="sourceFileName">Source FileName</param>
        /// <param name="sourceFullPath">Full Path of source file</param>
        /// <param name="destinationFullPath">Full Path of what will be destination</param>
        /// <param name="postProcessingSettings">Updated postprocessing settings for the source file</param>
        /// <returns>The JobId of the newly created job; -1, otherwise.</returns>
        public static int CreateEncodingJob(string sourceFileName, string sourceFullPath, string destinationFullPath, PostProcessingSettings postProcessingSettings)
        {
            int jobId = -1;
            if (ExistsByFileName(sourceFileName) is false)
            {
                EncodingJob newJob = new(IdNumber, sourceFullPath, destinationFullPath, postProcessingSettings);

                lock (jobLock)
                {
                    jobQueue.Add(newJob);
                    jobId = newJob.Id;
                }
            }

            return jobId;
        }
        /// <summary>Removes an encoding job from the list.</summary>
        /// <param name="job"><see cref="EncodingJob"/></param>
        /// <returns>True if successfully removed; False, otherwise.</returns>
        public static bool RemoveEncodingJob(EncodingJob job)
        {
            lock (jobLock)
            {
                return jobQueue.Remove(job);
            }
        }

        /// <summary>Removes an encoding job from the queue by id lookup.</summary>
        /// <param name="id">Id of the EncodingJob</param>
        /// <returns>True if successfully removed; False, otherwise.</returns>
        public static bool RemoveEncodingJobById(int id)
        {
            bool success = false;
            lock (jobLock)
            {
                var job = jobQueue.SingleOrDefault(x => x.Id == id);
                if (job is not null) success = jobQueue.Remove(job);
            }

            return success;
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
                return jobQueue.Exists(x => x.Id == id);
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
                                            x.NeedsPostProcessing &&
                                            (x.Paused is false) && (x.Error is false));
            }
        }

        /// <summary>Moves encoding job with given id up one in the list.</summary>
        /// <param name="jobId">Id of job to move</param>
        public static void MoveEncodingJobForward(int jobId)
        {
            int jobIndex = jobQueue.FindIndex(x => x.Id == jobId);
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
            int jobIndex = jobQueue.FindIndex(x => x.Id == jobId);

            // Already at the back of the list or not found
            if (jobIndex == (jobQueue.Count - 1) || jobIndex == -1) return;

            lock (jobLock)
            {
                (jobQueue[jobIndex + 1], jobQueue[jobIndex]) = (jobQueue[jobIndex], jobQueue[jobIndex + 1]);
            }
        }

        /// <summary>Gets encoding jobs that have been encoded and do not need post-processing. </summary>
        /// <returns>IReadOnlyList of <see cref="EncodingJob>"/></returns>
        public static IReadOnlyList<EncodingJob> GetEncodedEncodingJobs()
            => jobQueue.Where(x => x.Status >= EncodingJobStatus.ENCODED &&
                                                                    x.CompletedEncodingDateTime.HasValue &&
                                                                    x.PostProcessingFlags.Equals(PostProcessingFlags.None) is true).ToList();

        /// <summary>Gets encoding jobs that have been post-processed (and completed encoding). </summary>
        /// <returns>IReadOnlyList of <see cref="EncodingJob>"/></returns>
        public static IReadOnlyList<EncodingJob> GetPostProcessedEncodingJobs()
            => jobQueue.Where(x => x.Status.Equals(EncodingJobStatus.POST_PROCESSED) && x.CompletedPostProcessingTime.HasValue).ToList();

        /// <summary>Gets errored encoding jobs. </summary>
        /// <returns>IReadOnlyList of <see cref="EncodingJob"/></returns>
        public static IReadOnlyList<EncodingJob> GetErroredJobs() => jobQueue.Where(x => x.Error is true).ToList();

        public static string Output()
        { 
            string output = string.Empty;
            foreach (EncodingJob job in jobQueue)
            {
                output += $"{job.Id} - {job.FileName} ";
            }
            return output;
        }
    }
}