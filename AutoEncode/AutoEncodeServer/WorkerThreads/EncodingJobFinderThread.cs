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

        /// <summary>Forces thread to wake and rebuild source files </summary>
        /// <returns>Source Files</returns>
        public IDictionary<string, (bool IsShows, IEnumerable<SourceFileData> Files)> RequestSourceFiles()
        {
            Wake();

            Thread.Sleep(2);

            if (_buildingSourceFilesEvent.WaitOne(TimeSpan.FromSeconds(30)))
            {
                return SourceFiles.ToDictionary(x => x.Key, x => (x.Value.IsShows, x.Value.Files.AsEnumerable()));
            }

            return null;
        }

        public bool RequestEncodingJob(Guid guid)
        {
            bool success = false;

            // Wait for source file building if occurring
            if (_buildingSourceFilesEvent.WaitOne(TimeSpan.FromSeconds(30)))
            {
                if (SourceFilesByGuid.TryGetValue(guid, out (string Directory, SourceFileData File) sourceFile))
                {
                    if (string.IsNullOrWhiteSpace(sourceFile.Directory) is false && sourceFile.File is not null)
                    {
                        if (CreateEncodingJob(sourceFile.File, SearchDirectories[sourceFile.Directory].PostProcessing, SearchDirectories[sourceFile.Directory].Source) is true)
                        {
                            success = true;
                        }
                        else
                        {
                            Logger.LogError($"Failed to create encoding job for requested file {sourceFile.File.FullPath}");
                        }
                    }
                    else
                    {
                        Logger.LogError("Source file has invalid data.");
                    }
                }
                else
                {
                    Logger.LogError("CLIENT REQUEST: Failed to find source file to encode with the requested GUID.");
                }
            }

            return success;
        }
        #endregion Public Functions
    }
}
