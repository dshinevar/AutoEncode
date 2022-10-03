using AutomatedFFmpegServer.ServerSocket;
using AutomatedFFmpegServer.WorkerThreads;
using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AutomatedFFmpegServer
{
    public partial class AFServerMainThread
    {
        private static string ThreadName => "MainThread";
        private Task EncodingJobBuilderTask { get; set; }
        private CancellationTokenSource EncodingJobBuilderCancellationToken { get; set; }

        private Task EncodingTask { get; set; }
        private CancellationTokenSource EncodingCancellationToken { get; set; }

        private Task EncodingJobPostProcessingTask { get; set; }
        private CancellationTokenSource EncodingJobPostProcessingCancellationToken { get; set; }

        private EncodingJobFinderThread EncodingJobFinderThread { get; set; }
        private ManualResetEvent EncodingJobShutdown { get; set; } = new ManualResetEvent(false);

        // Maintenance Timer - Mean to run infrequently for cleanup tasks
        private readonly TimeSpan MaintenanceTimerWaitTime;
        private Timer MaintenanceTimer { get; set; }
        private ManualResetEvent MaintenanceTimerDispose { get; set; } = new ManualResetEvent(false);

        // Encoding Task Timer
        private readonly TimeSpan EncodingJobTaskTimerWaitTime;
        private Timer EncodingJobTaskTimer { get; set; }
        private ManualResetEvent EncodingJobTaskTimerDispose { get; set; } = new ManualResetEvent(false);

        // Process Timer - Processes Actions / Client Interactions
        private readonly TimeSpan ProcessTimerWaitTime;
        private Timer ProcessTimer { get; set; }
        private ManualResetEvent ProcessTimerDispose { get; set; } = new ManualResetEvent(false);
        private Queue<Action> TaskQueue { get; set; } = new Queue<Action>();

        private AFServerSocket ServerSocket { get; set; }
        /// <summary>Config as in file </summary>
        private AFServerConfig Config { get; set; }
        /// <summary>Config to be used; Does not have to match what is saved to file</summary>
        private AFServerConfig State { get; set; }
        private ManualResetEvent ShutdownMRE { get; set; }
        private Logger Logger { get; set; }

        /// <summary> Constructor; Creates Server Socket, Logger, JobFinderThread </summary>
        /// <param name="serverConfig">Server Config</param>
        public AFServerMainThread(AFServerConfig serverState, AFServerConfig serverConfig, Logger logger, ManualResetEvent shutdown)
        {
            State = serverState;
            Config = serverConfig;
            ShutdownMRE = shutdown;
            Logger = logger;
            ServerSocket = new AFServerSocket(this, Logger, Config.ServerSettings.IP, Config.ServerSettings.Port);
            EncodingJobFinderThread = new EncodingJobFinderThread(this, State, Logger, EncodingJobShutdown);

            MaintenanceTimerWaitTime = TimeSpan.FromHours(1);           // Doesn't need to run very often
            EncodingJobTaskTimerWaitTime = TimeSpan.FromSeconds(5);     // Run a bit slower than process; Is mainly managing the tasks so doesn't need to spin often
            ProcessTimerWaitTime = TimeSpan.FromSeconds(1.5);           // Handle processes pretty frequently
        }

        #region START/SHUTDOWN FUNCTIONS
        /// <summary> Starts Timers and Threads; Server socket starts listening. </summary>
        public void Start()
        {
            Debug.WriteLine("AFServerMainThread Starting");
            EncodingJobFinderThread.Start();
            //ServerSocket?.StartListening();

            MaintenanceTimer = new Timer(OnMaintenanceTimerElapsed, null, TimeSpan.FromHours(1), MaintenanceTimerWaitTime);
            EncodingJobTaskTimer = new Timer(OnEncodingJobTaskTimerElapsed, null, TimeSpan.FromSeconds(20), EncodingJobTaskTimerWaitTime);
            ProcessTimer = new Timer(OnProcessTimerElapsed, null, TimeSpan.FromSeconds(10), ProcessTimerWaitTime);
        }

        /// <summary>Shuts down AFServerMainThread; Disconnects server socket. </summary>
        public void Shutdown()
        {
            Debug.WriteLine("AFServerMainThread Shutting Down.");

            // Stop threads
            EncodingJobFinderThread?.Stop();
            EncodingJobBuilderCancellationToken?.Cancel();
            EncodingCancellationToken?.Cancel();
            EncodingJobPostProcessingCancellationToken?.Cancel();

            // Stop socket and timers
            ServerSocket.Disconnect(false);
            ServerSocket.Dispose();
            EncodingJobTaskTimer.Dispose(EncodingJobTaskTimerDispose);
            EncodingJobTaskTimerDispose.WaitOne();
            EncodingJobTaskTimerDispose.Dispose();

            MaintenanceTimer.Dispose(MaintenanceTimerDispose);
            MaintenanceTimerDispose.WaitOne();
            MaintenanceTimerDispose.Dispose();

            // Clear Task Queue and Stop processsing timer
            TaskQueue.Clear();
            ProcessTimer.Dispose(ProcessTimerDispose);
            ProcessTimerDispose.WaitOne();
            ProcessTimerDispose.Dispose();

            // Wait for threads to stop
            EncodingJobShutdown.WaitOne();
            EncodingJobBuilderTask?.Wait();
            EncodingTask?.Wait();
            EncodingJobPostProcessingTask?.Wait();
            ShutdownMRE.Set();
        }
        #endregion START/SHUTDOWN FUNCTIONS
    }
}
