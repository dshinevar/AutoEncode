using AutoEncodeUtilities;
using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AutoEncodeServer.WorkerThreads
{
    public partial class EncodingJobFinderThread
    {
        private bool DirectoryUpdate = false;
        private AutoResetEvent SleepARE { get; set; } = new AutoResetEvent(false);

        #region References
        /// <summary> Reference to the <see cref="AEServerMainThread"/></summary>
        protected AEServerMainThread MainThread { get; set; }
        /// <summary> Reference to the Server State</summary>
        protected AEServerConfig State { get; set; }
        /// <summary>Logger Reference</summary>
        protected ILogger Logger { get; set; }
        #endregion References

        /// <summary>Constructor</summary>
        /// <param name="mainThread">Main Thread handle <see cref="AEServerMainThread"/></param>
        /// <param name="serverState">Current Server State<see cref="AEServerConfig"/></param>
        public EncodingJobFinderThread(AEServerMainThread mainThread, AEServerConfig serverState, ILogger logger, ManualResetEvent shutdownMRE)
        {
            MainThread = mainThread;
            State = serverState;
            Logger = logger;
            ShutdownMRE = shutdownMRE;
            ThreadSleep = State.JobFinderSettings.ThreadSleep;
            SearchDirectories = State.Directories.ToDictionary(x => x.Key, x => x.Value.DeepClone());
        }

        #region Public Functions
        /// <summary>Signal to thread to update directories to search for jobs.</summary>
        public void UpdateSearchDirectories() => DirectoryUpdate = true;

        /// <summary>Gets a copy of video source files </summary>
        /// <returns></returns>
        public IDictionary<string, IEnumerable<SourceFileData>> GetMovieSourceFiles() => MovieSourceFiles.ToDictionary(x => x.Key, x => x.Value.AsEnumerable());

        /// <summary>Gets a copy of show source files</summary>
        /// <returns></returns>
        public IDictionary<string, IEnumerable<ShowSourceFileData>> GetShowSourceFiles() => ShowSourceFiles.ToDictionary(x => x.Key, x => x.Value.AsEnumerable());

        public (IDictionary<string, IEnumerable<SourceFileData>>, IDictionary<string, IEnumerable<ShowSourceFileData>>) RequestSourceFiles()
        {
            Wake();

            Thread.Sleep(2);

            bool finished = _buildingSourceFilesEvent.WaitOne(TimeSpan.FromSeconds(30));

            if (finished is false) return (null, null);

            return (GetMovieSourceFiles(), GetShowSourceFiles());
        }

        public bool RequestEncodingJob(Guid guid, bool isShow)
        {
            bool success = false;

            if (isShow is true)
            {
                (string directory, ShowSourceFileData fileToEncode) = ShowSourceFiles.SelectMany(kvp => kvp.Value, (kvp, file) => (kvp.Key, file)).FirstOrDefault(x => x.file.Guid == guid);

                if (fileToEncode is not null) 
                {
                    if (CreateEncodingJob(fileToEncode, SearchDirectories[directory].PostProcessing, SearchDirectories[directory].Source) is true)
                    {
                        success = true;
                    }
                    else
                    {
                        Logger.LogError($"Failed to create encoding job for requested file {fileToEncode.FullPath}");
                    }
                }
                else
                {
                    Logger.LogError("CLIENT REQUEST: Failed to find an episode of a show to encode with the requested GUID.");
                }
            }
            else
            {
                (string directory, SourceFileData fileToEncode) = MovieSourceFiles.SelectMany(kvp => kvp.Value, (kvp, file) => (kvp.Key, file)).FirstOrDefault(x => x.file.Guid == guid);

                if (fileToEncode is not null)
                {
                    if (CreateEncodingJob(fileToEncode, SearchDirectories[directory].PostProcessing, SearchDirectories[directory].Source) is true)
                    {
                        success = true;
                    }
                    else
                    {
                        Logger.LogError($"Failed to create encoding job for requested file {fileToEncode.FullPath}");
                    }
                }
                else
                {
                    Logger.LogError("CLIENT REQUEST: Failed to find movie to encode with the requested GUID.");
                }
            }

            return success;
        }
        #endregion Public Functions
    }
}
