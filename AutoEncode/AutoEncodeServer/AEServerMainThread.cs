using AutoEncodeServer.Comm;
using AutoEncodeServer.WorkerThreads;
using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace AutoEncodeServer
{
    public partial class AEServerMainThread
    {
        public readonly string ThreadName = "MainThread";

        /// <summary>Config as in file </summary>
        private AEServerConfig Config { get; set; }
        /// <summary>Config to be used; Does not have to match what is saved to file</summary>
        private AEServerConfig State { get; set; }
        private ManualResetEvent ShutdownMRE { get; set; }
        private ILogger Logger { get; set; }

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

        private ClientUpdateService ClientUpdateService { get; set; }

        private CommunicationManager CommunicationManager { get; set; }

        /// <summary> Constructor; Creates Server Socket, Logger, JobFinderThread </summary>
        /// <param name="serverConfig">Server Config</param>
        public AEServerMainThread(AEServerConfig serverState, AEServerConfig serverConfig, ILogger logger, ManualResetEvent shutdown)
        {
            State = serverState;
            Config = serverConfig;
            ShutdownMRE = shutdown;
            Logger = logger;
            EncodingJobFinderThread = new EncodingJobFinderThread(this, State, Logger, EncodingJobShutdown);
            ClientUpdateService = new(Logger, Config.ConnectionSettings.ClientUpdatePort);
            CommunicationManager = new(this, Logger, Config.ConnectionSettings.CommunicationPort);

            MaintenanceTimerWaitTime = TimeSpan.FromSeconds(45);        // Doesn't need to run as often
            EncodingJobTaskTimerWaitTime = TimeSpan.FromSeconds(5);     // Run a bit slower than process; Is mainly managing the tasks so doesn't need to spin often
        }

        #region START/SHUTDOWN FUNCTIONS
        /// <summary> Starts Timers and Threads; Server socket starts listening. </summary>
        public void Start()
        {
            Debug.WriteLine("AEServerMainThread Starting");
            EncodingJobFinderThread.Start();

            MaintenanceTimer = new Timer(OnMaintenanceTimerElapsed, null, TimeSpan.FromMinutes(1), MaintenanceTimerWaitTime);
            EncodingJobTaskTimer = new Timer(OnEncodingJobTaskTimerElapsed, null, TimeSpan.FromSeconds(20), EncodingJobTaskTimerWaitTime);

            ClientUpdateService?.Initialize();
            CommunicationManager?.Start();
        }

        /// <summary>Shuts down AEServerMainThread; Disconnects Comms </summary>
        public void Shutdown()
        {
            Debug.WriteLine("AEServerMainThread Shutting Down.");

            // Stop Timers timers
            EncodingJobTaskTimer?.Dispose(EncodingJobTaskTimerDispose);
            EncodingJobTaskTimerDispose.WaitOne();
            EncodingJobTaskTimerDispose.Dispose();

            MaintenanceTimer?.Dispose(MaintenanceTimerDispose);
            MaintenanceTimerDispose.WaitOne();
            MaintenanceTimerDispose.Dispose();

            // Stop Comms
            ClientUpdateService?.Shutdown();
            CommunicationManager?.Stop();

            // Stop threads
            EncodingJobBuilderCancellationToken?.Cancel();
            EncodingCancellationToken?.Cancel();
            EncodingJobPostProcessingCancellationToken?.Cancel();
            EncodingJobFinderThread?.Stop();

            // Wait for threads to stop
            EncodingJobShutdown.WaitOne();
            EncodingJobBuilderTask?.Wait();
            EncodingTask?.Wait();
            EncodingJobPostProcessingTask?.Wait();
            ShutdownMRE.Set();
        }
        #endregion START/SHUTDOWN FUNCTIONS

        #region PROCESSING
        public (IDictionary<string, IEnumerable<SourceFileData>> Movies, IDictionary<string, IEnumerable<ShowSourceFileData>> Shows) RequestSourceFiles() => EncodingJobFinderThread.RequestSourceFiles();
        public bool RequestEncodingJob(Guid guid, bool isShow) => EncodingJobFinderThread.RequestEncodingJob(guid, isShow);
        #endregion PROCESSING
    }
}
