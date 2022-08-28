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

        private readonly int ServerTimerWaitTime;
        private Timer ServerTimer { get; set; }
        private ManualResetEvent ServerTimerDispose { get; set; } = new ManualResetEvent(false);

        private readonly int TaskTimerWaitTime;
        private Timer TaskTimer { get; set; }
        private ManualResetEvent TaskTimerDispose { get; set; } = new ManualResetEvent(false);
        private Queue<Action> TaskQueue { get; set; } = new Queue<Action>();

        private AFServerSocket ServerSocket { get; set; }
        private AFServerConfig Config { get; set; }
        private ManualResetEvent ShutdownMRE { get; set; }
        private Logger Logger { get; set; }

        /// <summary> Constructor; Creates Server Socket, Logger, JobFinderThread </summary>
        /// <param name="serverConfig">Server Config</param>
        public AFServerMainThread(AFServerConfig serverConfig, Logger logger, ManualResetEvent shutdown)
        {
            Config = serverConfig;
            ShutdownMRE = shutdown;
            Logger = logger;
            ServerSocket = new AFServerSocket(this, Logger, Config.ServerSettings.IP, Config.ServerSettings.Port);
            EncodingJobFinderThread = new EncodingJobFinderThread(this, Config, Logger, EncodingJobShutdown);

            TaskTimerWaitTime = 1000;
            ServerTimerWaitTime = 1000;
        }

        #region START/SHUTDOWN FUNCTIONS
        /// <summary> Starts AFServerMainThread; Server socket starts listening. </summary>
        public void Start()
        {
            Debug.WriteLine("AFServerMainThread Starting");
            EncodingJobFinderThread.Start();
            //ServerSocket?.StartListening();
            TaskTimer = new Timer(OnTaskTimerElapsed, null, 15000, TaskTimerWaitTime);
            ServerTimer = new Timer(OnServerTimerElapsed, null, 10000, ServerTimerWaitTime);
        }

        /// <summary>Shuts down AFServerMainThread; Disconnects server socket. </summary>
        public void Shutdown()
        {
            Debug.WriteLine("AFServerMainThread Shutting Down.");

            // Stop threads
            EncodingJobFinderThread?.Stop();
            EncodingJobBuilderCancellationToken.Cancel();
            EncodingCancellationToken.Cancel();
            EncodingJobPostProcessingCancellationToken.Cancel();

            // Stop socket and timers
            ServerSocket.Disconnect(false);
            ServerSocket.Dispose();
            ServerTimer.Dispose(ServerTimerDispose);
            ServerTimerDispose.WaitOne();
            ServerTimerDispose.Dispose();

            // Clear and stop task queue
            TaskQueue.Clear();
            TaskTimer.Dispose(TaskTimerDispose);
            TaskTimerDispose.WaitOne();
            TaskTimerDispose.Dispose();

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
