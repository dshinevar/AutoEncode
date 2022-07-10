using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using System.Collections.Generic;
using System.Linq;

namespace AutomatedFFmpegServer
{
    public static class EncodingJobQueue
    {
        private static List<EncodingJob> jobQueue = new List<EncodingJob>();
        private static readonly object jobLock = new object();
        private static int _idNumber = 1;
        private static int idNumber 
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

        /// <summary>Adds an encoding job to the list.</summary>
        /// <param name="job">EncodingJob</param>
        public static void AddEncodingJob(EncodingJob job)
        {
            lock (jobLock)
            {
                job.JobId = idNumber;
                jobQueue.Add(job);
            }
        }
        /// <summary>Removes an encoding job from the list.</summary>
        /// <param name="job">EncodingJob</param>
        public static void RemoveEncodingJob(EncodingJob job)
        {
            lock (jobLock)
            {
                jobQueue.Remove(job);
            }
        }

        public static bool ExistsByFileName(string filename)
        {
            lock (jobLock)
            {
                return jobQueue.Exists(x => x.FileName.Equals(filename));
            }
        }

        public static bool ExistsById(int id)
        {
            lock (jobLock)
            {
                return jobQueue.Exists(x => x.JobId == id);
            }
        }

        /// <summary> Gets first EncodingJob from list with the given status. </summary>
        /// <param name="status">EncodingJobStatus</param>
        public static EncodingJob GetNextEncodingJobWithStatus(EncodingJobStatus status)
        {
            lock (jobLock)
            {
                return jobQueue.Find(x => x.Status.Equals(status) && (x.Paused is false));
            }
        }
        /// <summary>Moves encoding job at given index up one in the list.</summary>
        /// <param name="jobIndex">Index of job to move</param>
        public static void MoveEncodingJobForward(int jobIndex)
        {
            // Already at the front of the list
            if (jobIndex == 0) return;

            lock (jobLock)
            {
                EncodingJob tmp = jobQueue[jobIndex];
                jobQueue[jobIndex] = jobQueue[jobIndex - 1];
                jobQueue[jobIndex - 1] = tmp;
            }
        }
        /// <summary>Moves encoding job at given index back one in the list.</summary>
        /// <param name="jobIndex">Index of job to move</param>
        public static void MoveEncodingJobBack(int jobIndex)
        {
            // Already at the back of the list
            if (jobIndex == (jobQueue.Count - 1)) return;

            lock (jobLock)
            {
                EncodingJob tmp = jobQueue[jobIndex];
                jobQueue[jobIndex] = jobQueue[jobIndex + 1];
                jobQueue[jobIndex + 1] = tmp;
            }
        }

        public static string Output()
        { 
            string output = string.Empty;
            foreach (EncodingJob job in jobQueue)
            {
                output += $"{job.FileName} ";
            }
            return output;
        }
    }
}
