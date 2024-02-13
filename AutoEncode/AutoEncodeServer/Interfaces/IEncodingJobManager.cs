using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Data;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AutoEncodeServer.Interfaces
{
    public interface IEncodingJobManager
    {
        /// <summary>Number of jobs in queue</summary>
        int Count { get; }

        /// <summary>Sets up the manager and client update publisher</summary>
        /// <param name="serverState">Current state of the server</param>
        /// <param name="shutdownMRE"><see cref="ManualResetEvent"/> used to indicate when shut down</param>
        /// <exception cref="Exception">Rethrows exceptions.</exception>
        void Initialize(AEServerConfig serverState, ManualResetEvent shutdownMRE);

        /// <summary>Starts up timer and publisher</summary>
        /// <exception cref="Exception">Rethrows exceptions.</exception>
        void Start();

        /// <summary>Stops and disposes timer and publisher</summary>
        /// <exception cref="Exception">Rethrows exceptions.</exception>
        void Shutdown();

        /// <summary>Gets the current state of the queue</summary>
        /// <returns>List of <see cref="EncodingJobData"/></returns>
        IEnumerable<EncodingJobData> GetEncodingJobQueue();

        /// <summary>If able, creats and adds encoding job to queue.</summary>
        /// <param name="sourceFileData"><see cref="SourceFileData"/></param>
        /// <param name="postProcessingSettings"><see cref="PostProcessingSettings"/></param>
        /// <returns>Id of job if created.</returns>
        ulong? CreateEncodingJob(SourceFileData sourceFileData, PostProcessingSettings postProcessingSettings);

        /// <summary>Determines if a job exists by the given filename and is currently encoding.</summary>
        /// <param name="filename"></param>
        /// <returns>True if exists and encoding; False, otherwise.</returns>
        bool IsEncodingByFileName(string filename);

        /// <summary>Removes an encoding job from the queue by id lookup.</summary>
        /// <param name="id">Id of the EncodingJob</param>
        /// <returns>True if successfully removed; False, otherwise.</returns>
        bool RemoveEncodingJobById(ulong id);

        #region Job Methods
        /// <summary>Cancels the given job's currently running task</summary>
        /// <param name="jobId">Id of job.</param>
        /// <returns>True if successful.</returns>
        bool CancelJob(ulong jobId);

        /// <summary>Pauses the given job.</summary>
        /// <param name="jobId">Id of job.</param>
        /// <returns>True if successful.</returns>
        bool PauseJob(ulong jobId);

        /// <summary>Resumes the given job if paused.</summary>
        /// <param name="jobId">Id of job</param>
        /// <returns>True if successful.</returns>
        bool ResumeJob(ulong jobId);

        /// <summary>Cancels then pauses the given job.</summary>
        /// <param name="jobId">Id of job.</param>
        /// <returns>True if successful.</returns>
        bool CancelThenPauseJob(ulong jobId);
        #endregion Job Methods
    }
}
