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

        /// <summary>Creates an EncodingJob and adds to queue based off the given info. </summary>
        /// <param name="videoSourceData"><see cref="VideoSourceData"/></param>
        /// <param name="sourceDirectoryPath">Directory path of source</param>
        /// <param name="destinationDirectoryPath">Directory path of destination</param>
        public static void CreateEncodingJob(VideoSourceData videoSourceData, string sourceDirectoryPath, string destinationDirectoryPath)
        {
            if (!ExistsByFileName(videoSourceData.FileName))
            {
                EncodingJob newJob = new EncodingJob()
                {
                    JobId = idNumber,
                    FileName = videoSourceData.FileName,
                    SourceFullPath = videoSourceData.FullPath,
                    DestinationFullPath = videoSourceData.FullPath.Replace(sourceDirectoryPath, destinationDirectoryPath)
                };
                lock (jobLock)
                {
                    jobQueue.Add(newJob);
                }
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
        /// <summary>Moves encoding job at given index back one in the list.</summary>
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
