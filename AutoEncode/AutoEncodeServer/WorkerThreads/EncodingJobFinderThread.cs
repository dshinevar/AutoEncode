using AutoEncodeUtilities;
using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AutoEncodeServer.WorkerThreads
{
    public partial class EncodingJobFinderThread
    {
        private bool Shutdown = false;
        private bool DirectoryUpdate = false;
        private ManualResetEvent ShutdownMRE { get; set; }
        private AutoResetEvent SleepARE { get; set; } = new AutoResetEvent(false);

        private readonly object movieSourceFileLock = new();
        private readonly object showSourceFileLock = new();
        private Dictionary<string, SearchDirectory> SearchDirectories { get; set; }
        private Dictionary<string, List<VideoSourceData>> MovieSourceFiles { get; set; } = new Dictionary<string, List<VideoSourceData>>();
        private Dictionary<string, List<ShowSourceData>> ShowSourceFiles { get; set; } = new Dictionary<string, List<ShowSourceData>>();

        private Thread Thread { get; set; }
        private string ThreadName => Thread?.Name ?? string.Empty;
        private TimeSpan ThreadSleep { get; set; } = TimeSpan.FromMinutes(2);

        private AEWorkerThreadStatus Status { get; set; } = AEWorkerThreadStatus.PROCESSING;
        private AEServerMainThread MainThread { get; set; }
        private AEServerConfig State { get; set; }
        private Logger Logger { get; set; }

        /// <summary>Constructor</summary>
        /// <param name="mainThread">Main Thread handle <see cref="AEServerMainThread"/></param>
        /// <param name="serverState">Current Server State<see cref="AEServerConfig"/></param>
        public EncodingJobFinderThread(AEServerMainThread mainThread, AEServerConfig serverState, Logger logger, ManualResetEvent shutdownMRE)
        {
            MainThread = mainThread;
            State = serverState;
            Logger = logger;
            ShutdownMRE = shutdownMRE;
            ThreadSleep = State.JobFinderSettings.ThreadSleep;
            SearchDirectories = State.Directories.ToDictionary(x => x.Key, x => x.Value.DeepClone());
        }

        #region Start/Stop Functions
        public void Start()
        {
            Thread = new Thread(() => ThreadLoop())
            {
                Name = nameof(EncodingJobFinderThread),
                IsBackground = true
            };

            Logger.LogInfo($"{ThreadName} Starting", ThreadName);
            // Update the source files initially before starting thread
            BuildSourceFiles(SearchDirectories);
            Thread.Start();
        }

        public void Stop()
        {
            Logger.LogInfo($"{ThreadName} Shutting Down", ThreadName);
            Shutdown = true;

            Wake();
            Thread.Join();

            ShutdownMRE.Set();
        }
        #endregion Start/Stop Functions

        #region Thread Functions
        /// <summary> Wakes up thread by setting the Sleep AutoResetEvent.</summary>
        public void Wake() => SleepARE.Set();

        /// <summary> Sleeps thread for certain amount of time. </summary>
        private void Sleep()
        {
            Status = AEWorkerThreadStatus.SLEEPING;
            SleepARE.WaitOne(ThreadSleep);
        }
        #endregion Thread Functions

        #region Public Functions
        public ThreadStatusData GetThreadStatus() => new ThreadStatusData(ThreadName, Status);

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
