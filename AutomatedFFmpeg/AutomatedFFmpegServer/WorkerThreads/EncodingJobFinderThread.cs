using AutomatedFFmpegUtilities;
using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AutomatedFFmpegServer.WorkerThreads
{
    public partial class EncodingJobFinderThread
    {
        private const int MaxFailedToFindJobCount = 6;
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
        private TimeSpan ThreadSleep { get; set; } = TimeSpan.FromMinutes(5);
        private TimeSpan ThreadDeepSleep => TimeSpan.FromTicks(ThreadSleep.Ticks * 5);

        private AFWorkerThreadStatus Status { get; set; } = AFWorkerThreadStatus.PROCESSING;
        private AFServerMainThread MainThread { get; set; }
        private AFServerConfig State { get; set; }
        private Logger Logger { get; set; }

        /// <summary>Constructor</summary>
        /// <param name="mainThread">Main Thread handle <see cref="AFServerMainThread"/></param>
        /// <param name="serverState">Current Server State<see cref="AFServerConfig"/></param>
        public EncodingJobFinderThread(AFServerMainThread mainThread, AFServerConfig serverState, Logger logger, ManualResetEvent shutdownMRE)
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
            Status = AFWorkerThreadStatus.SLEEPING;
            SleepARE.WaitOne(ThreadSleep);
        }

        /// <summary> Sleeps thread for 5x length of sleep. </summary>
        private void DeepSleep()
        {
            Status = AFWorkerThreadStatus.DEEP_SLEEPING;
            SleepARE.WaitOne(ThreadDeepSleep);
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
