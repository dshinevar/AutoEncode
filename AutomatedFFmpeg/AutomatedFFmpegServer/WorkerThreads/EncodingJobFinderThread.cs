using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Logger;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace AutomatedFFmpegServer.WorkerThreads
{
    public partial class EncodingJobFinderThread
    {
        private const int MAX_COUNT = 6;
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
        private int ThreadSleep { get; set; } = 5000;
        private int ThreadDeepSleep => ThreadSleep * 5;

        private AFWorkerThreadStatus Status { get; set; } = AFWorkerThreadStatus.PROCESSING;
        private AFServerMainThread MainThread { get; set; }
        private AFServerConfig Config { get; set; }
        private Logger Logger { get; set; }

        /// <summary>Constructor</summary>
        /// <param name="mainThread">Main Thread handle <see cref="AFServerMainThread"/></param>
        /// <param name="serverConfig">Config <see cref="AFServerConfig"/></param>
        public EncodingJobFinderThread(AFServerMainThread mainThread, AFServerConfig serverConfig, Logger logger, ManualResetEvent shutdownMRE)
        {
            MainThread = mainThread;
            Config = serverConfig;
            Logger = logger;
            ShutdownMRE = shutdownMRE;
            ThreadSleep = Config.ServerSettings.ThreadSleepInMS;
            SearchDirectories = Config.Directories.ToDictionary(x => x.Key, x => (SearchDirectory)x.Value.Clone());
        }

        #region Start/Stop Functions
        public void Start()
        {
            void threadStart() => ThreadLoop();
            Thread = new Thread(threadStart)
            {
                Name = nameof(EncodingJobFinderThread),
                IsBackground = true
            };

            Debug.WriteLine($"{ThreadName} Starting");
            Logger.LogInfo($"{ThreadName} Starting", ThreadName);
            // Update the source files initially before starting thread
            BuildSourceFiles(SearchDirectories);
            Thread.Start();
        }

        public void Stop()
        {
            Debug.WriteLine($"{ThreadName} Shutting Down.");
            Logger.LogInfo($"{ThreadName} Shutting Down", ThreadName);
            Shutdown = true;

            Wake();
            Thread.Join();

            ShutdownMRE.Set();
        }
        #endregion Start/Stop Functions

        #region Thread Functions
        /// <summary> Wakes up thread by setting the Sleep AutoResetEvent. Not used by default.</summary>
        public void Wake()
        {
            SleepARE.Set();
            Status = AFWorkerThreadStatus.PROCESSING;
        }

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
                return MovieSourceFiles.ToDictionary(x => x.Key, x => x.Value.Select(v => new VideoSourceData(v)).ToList());
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
