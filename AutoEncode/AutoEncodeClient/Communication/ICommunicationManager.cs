using AutoEncodeUtilities.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoEncodeClient.Communication
{
    public interface ICommunicationManager
    {
        /// <summary>Initializes Communication Manager</summary>
        /// <param name="ipAddress">IP Address to connect to</param>
        /// <param name="port">Port to connect to</param>
        void Initialize(string ipAddress, int port);

        /// <summary>Formatted connection string used for sockets -- takes in IpAddress and Port</summary>
        string ConnectionString { get; }

        /// <summary>IP Address to connect to</summary>
        string IpAddress { get; }

        /// <summary>Port to connect to</summary>
        int Port { get; }

        /// <summary>Sends a request for source files.</summary>
        /// <returns>Dictionary containing library/directory (key) and it's source files (value)</returns>
        Task<IDictionary<string, (bool IsShows, IEnumerable<SourceFileData> Files)>> RequestSourceFiles();

        /// <summary>Sends a request to cancel a job.</summary>
        /// <param name="jobId">Id of the job to cancel.</param>
        /// <returns>True if successful; False, otherwise.</returns>
        Task<bool> CancelJob(ulong jobId);

        /// <summary>Sends a request to pause a job.</summary>
        /// <param name="jobId">Id of the job to pause.</param>
        /// <returns>True if successful; False, otherwise.</returns>
        Task<bool> PauseJob(ulong jobId);

        /// <summary>Sends a request to resume a job.</summary>
        /// <param name="jobId">Id of the job to resume.</param>
        /// <returns>True if successful; False, otherwise.</returns>
        Task<bool> ResumeJob(ulong jobId);

        /// <summary>Sends a request to cancel then pause a job.</summary>
        /// <param name="jobId">Id of the job to cancel then pause.</param>
        /// <returns>True if successful; False, otherwise.</returns>
        Task<bool> CancelThenPauseJob(ulong jobId);

        /// <summary>Sends a request to add a job to encoding queue.</summary>
        /// <param name="sourceFileGuid"><see cref="Guid"/> of the source file.</param>
        /// <returns>True if successful; False, otherwise.</returns>
        Task<bool> RequestEncode(Guid sourceFileGuid);

        /// <summary>Sends a request to remove a job from the encoding queue.</summary>
        /// <param name="jobId">Id of the job to remove.</param>
        /// <returns>True if successful; False, otherwise.</returns>
        Task<bool> RequestRemoveJob(ulong jobId);

        /// <summary>Requests the current job queue state -- mainly meant for ONLY on startup in order to get current state. </summary>
        /// <returns>List of <see cref="EncodingJobData"/></returns>
        Task<IEnumerable<EncodingJobData>> RequestJobQueue();
    }
}
