using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using System.Collections.Generic;
using System.Linq;

namespace AutomatedFFmpegServer
{
    public class EncodingJobs
    {
        private List<EncodingJob> _jobList = new List<EncodingJob>();
        private readonly object _lock = new object();

        public EncodingJobs() { }

        /// <summary>Gets current list of encoding jobs. </summary>
        /// <returns>EncodingJob list</returns>
        public List<EncodingJob> GetEncodingJobs()
        {
            lock (_lock)
            {
                return _jobList;
            }
        }

        public List<EncodingJobClientData> GetEncodingJobsForClient()
        {
            lock (_lock)
            {
                return _jobList.Count > 0 ? _jobList.ConvertAll(x => new EncodingJobClientData(x)).ToList() : new List<EncodingJobClientData>();
            }
        }

        /// <summary>Adds an encoding job to the list.</summary>
        /// <param name="job">EncodingJob</param>
        public void AddEncodingJob(EncodingJob job)
        {
            lock (_lock)
            {
                _jobList.Add(job);
            }
        }
        /// <summary>Removes an encoding job from the list.</summary>
        /// <param name="job">EncodingJob</param>
        public void RemoveEncodingJob(EncodingJob job)
        {
            lock (_lock)
            {
                _jobList.Remove(job);
            }
        }

        public bool Exists(EncodingJob job)
        {
            lock (_lock)
            {
                return _jobList.Exists(x => x.Name == job.Name);
            }
        }
        /// <summary> Gets first EncodingJob from list with the given status. </summary>
        /// <param name="status">EncodingJobStatus</param>
        public EncodingJob GetNextEncodingJobWithStatus(EncodingJobStatus status)
        {
            lock (_lock)
            {
                return _jobList.Find(x => x.Status.Equals(status));
            }
        }
        /// <summary>Moves encoding job at given index up one in the list.</summary>
        /// <param name="jobIndex">Index of job to move</param>
        public void MoveEncodingJobForward(int jobIndex)
        {
            // Already at the front of the list
            if (jobIndex == 0) return;

            lock (_lock)
            {
                EncodingJob tmp = _jobList[jobIndex];
                _jobList[jobIndex] = _jobList[jobIndex - 1];
                _jobList[jobIndex - 1] = tmp;
            }
        }
        /// <summary>Moves encoding job at given index back one in the list.</summary>
        /// <param name="jobIndex">Index of job to move</param>
        public void MoveEncodingJobBack(int jobIndex)
        {
            // Already at the back of the list
            if (jobIndex == (_jobList.Count - 1)) return;

            lock (_lock)
            {
                EncodingJob tmp = _jobList[jobIndex];
                _jobList[jobIndex] = _jobList[jobIndex + 1];
                _jobList[jobIndex + 1] = tmp;
            }
        }

        public override string ToString()
        { 
            string output = string.Empty;
            foreach (EncodingJob job in _jobList)
            {
                output += $"{job.Name} ";
            }
            return output;
        }
    }
}
