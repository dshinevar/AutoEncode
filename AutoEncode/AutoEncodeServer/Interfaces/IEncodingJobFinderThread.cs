using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Data;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AutoEncodeServer.Interfaces
{
    public interface IEncodingJobFinderThread
    {
        void Initialize(AEServerConfig serverState, ManualResetEvent shutdownMRE);

        void Start();

        void Stop();

        /// <summary>Signal to thread to update directories to search for jobs.</summary>
        void UpdateSearchDirectories();

        /// <summary>Forces thread to wake and rebuild source files </summary>
        /// <returns>Source Files</returns>
        IDictionary<string, (bool IsShows, IEnumerable<SourceFileData> Files)> RequestSourceFiles();

        bool RequestEncodingJob(Guid guid);
    }
}
