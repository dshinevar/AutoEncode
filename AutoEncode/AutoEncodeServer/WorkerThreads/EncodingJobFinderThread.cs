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

        private readonly object movieSourceFileLock = new();
        private readonly object showSourceFileLock = new();
        private Dictionary<string, SearchDirectory> SearchDirectories { get; set; }
        private Dictionary<string, List<VideoSourceData>> MovieSourceFiles { get; set; } = new Dictionary<string, List<VideoSourceData>>();
        private Dictionary<string, List<ShowSourceData>> ShowSourceFiles { get; set; } = new Dictionary<string, List<ShowSourceData>>();

        private TimeSpan ThreadSleep { get; set; } = TimeSpan.FromMinutes(2);



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
        public Dictionary<string, List<VideoSourceData>> GetMovieSourceFiles()
        {
            lock (movieSourceFileLock)
            {
                return MovieSourceFiles.ToDictionary(x => x.Key, x => x.Value.Select(v => v.DeepClone()).ToList());
            }
        }

        /// <summary>Gets a copy of show source files</summary>
        /// <returns></returns>
        public Dictionary<string, List<ShowSourceData>> GetShowSourceFiles()
        {
            lock (showSourceFileLock)
            {
                return ShowSourceFiles.ToDictionary(x => x.Key, x => x.Value.Select(s => s.DeepClone()).ToList());
            }
        }
        #endregion Public Functions
    }
}
