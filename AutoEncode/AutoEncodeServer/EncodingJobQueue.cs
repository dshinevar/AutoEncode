using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System.Collections.Generic;
using System.Linq;

namespace AutoEncodeServer
{
    public static class EncodingJobQueue
    {
        private static readonly List<EncodingJob> jobQueue = new();
        private static readonly object jobLock = new();
        private static ulong _idNumber = 1;
        private static ulong IdNumber
        {
            get
            {
                ulong tmp = _idNumber;
                _idNumber++;
                return tmp;
            }
        }

        public static bool Any() => jobQueue.Any();

        public static int Count => jobQueue.Count;

        public static List<EncodingJobData> GetEncodingJobsData()
        {
            lock (jobLock)
            {
                return jobQueue.Select(x => x.ExportData()).ToList();
            }
        }

        /// <summary>Creates an EncodingJob and adds to queue based off the given info. </summary>
        /// <param name="sourceFileData"><see cref="SourceFileData"/> with basic data to create a job.</param>
        /// <param name="postProcessingSettings">Updated postprocessing settings for the source file</param>
        /// <returns>The JobId of the newly created job; Null, otherwise.</returns>
        public static ulong? CreateEncodingJob(SourceFileData sourceFileData, PostProcessingSettings postProcessingSettings)
        {
            ulong? jobId = null;
            if (ExistsByFileName(sourceFileData.FileName) is false)
            {
                jobId = IdNumber;
                EncodingJob newJob = new((ulong)jobId, sourceFileData, postProcessingSettings);

                lock (jobLock)
                {
                    jobQueue.Add(newJob);
                }
            }

            return jobId;
        }

        /// <summary>Removes an encoding job from the queue by id lookup.</summary>
        /// <param name="id">Id of the EncodingJob</param>
        /// <returns>True if successfully removed; False, otherwise.</returns>
        public static bool RemoveEncodingJobById(ulong? id)
        {
            bool success = false;

            if (id is not null)
            {
                lock (jobLock)
                {
                    var job = jobQueue.SingleOrDefault(x => x.Id == id);
                    if (job is not null) success = jobQueue.Remove(job);
                }
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
        public static bool ExistsById(ulong id)
        {
            lock (jobLock)
            {
                return jobQueue.Exists(x => x.Id == id);
            }
        }

        public static bool IsEncodingByFileName(string filename)
        {
            lock (jobLock)
            {
                return jobQueue.Find(x => x.FileName.Equals(filename))?.Status.Equals(EncodingJobStatus.ENCODING) ?? false;
            }
        }

        public static EncodingJob GetEncodingJobById(ulong id)
        {
            lock (jobLock)
            {
                return jobQueue.Find(x => x.Id == id);
            }
        }

        /// <summary> Gets first EncodingJob (not paused or in error) from list with the given status. </summary>
        /// <param name="status">EncodingJobStatus</param>
        /// <returns><see cref="EncodingJob"/></returns>
        public static EncodingJob GetNextEncodingJobWithStatus(EncodingJobStatus status)
        {
            lock (jobLock)
            {
                return jobQueue.Find(x => x.Status.Equals(status) && (x.Paused is false) && (x.Error is false) && (x.Cancelled is false));
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
        public static void MoveEncodingJobForward(ulong jobId)
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
        public static void MoveEncodingJobBack(ulong jobId)
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

        public new static string ToString()
        {
            string output = string.Empty;
            foreach (EncodingJob job in jobQueue)
            {
                output += $"{job.Id} - {job.FileName} ";
            }
            return output;
        }

        #region Actions On Encoding Jobs
        public static bool CancelJob(ulong jobId)
        {
            bool success = false;
            lock (jobLock)
            {
                EncodingJob job = jobQueue.Find(x => x.Id == jobId);

                if (job is not null)
                {
                    job.Cancel();
                    success = true;
                }
            }

            return success;
        }

        public static bool PauseJob(ulong jobId)
        {
            bool success = false;
            lock (jobLock)
            {
                EncodingJob job = jobQueue.Find(x => x.Id == jobId);

                if (job is not null)
                {
                    job.Pause();
                    success = true;
                }
            }

            return success;
        }

        public static bool ResumeJob(ulong jobId)
        {
            bool success = false;
            lock (jobLock)
            {
                EncodingJob job = jobQueue.Find(x => x.Id == jobId);

                if (job is not null)
                {
                    job.Resume();
                    success = true;
                }
            }

            return success;
        }

        public static bool CancelThenPauseJob(ulong jobId)
        {
            bool success = false;
            lock (jobLock)
            {
                EncodingJob job = jobQueue.Find(x => x.Id == jobId);
                if (job is not null)
                {
                    job.Cancel();
                    job.Pause();
                    success = true;
                }
            }

            return success;
        }
        #endregion Actions On Encoding Jobs
    }
}
