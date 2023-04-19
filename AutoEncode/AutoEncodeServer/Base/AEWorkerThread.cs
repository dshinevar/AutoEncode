using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using System;
using System.Threading;

namespace AutoEncodeServer.Base
{
    /// <summary>Base Class for AutoEncode Threads </summary>
    public abstract class AEWorkerThread
    {
        #region Shutdown Properties
        /// <summary>Shutdown flag</summary>
        protected bool Shutdown = false;
        /// <summary>Shutdown MRE from MainThread; Signals to MainThread that Shutdown is complete.</summary>
        private ManualResetEvent ShutdownMRE { get; set; }
        #endregion Shutdown Properties

        #region Thread Properties
        /// <summary><see cref="Thread"/> Object</summary>
        private Thread WorkerThread { get; set; }
        private string _threadName = nameof(AEWorkerThread);
        /// <summary>Name of the Worker Thread</summary>
        protected string ThreadName => WorkerThread?.Name ?? _threadName;
        /// <summary>The status of the Worker Thread</summary>
        protected AEWorkerThreadStatus ThreadStatus { get; set; } = AEWorkerThreadStatus.Processing;
        #endregion Thread Properties

        #region References
        /// <summary> Reference to the <see cref="AEServerMainThread"/></summary>
        protected AEServerMainThread MainThread { get; set; }
        /// <summary> Reference to the Server State</summary>
        protected AEServerConfig State { get; set; }
        /// <summary>Logger Reference</summary>
        protected ILogger Logger { get; set; }
        #endregion References

        /// <summary>Constructor</summary>
        /// <param name="threadName">Name of worker thread</param>
        /// <param name="mainThread"><see cref="AEServerMainThread"/> reference</param>
        /// <param name="state">Server State reference</param>
        /// <param name="logger"><see cref="ILogger"/></param>
        /// <param name="shutdownMRE">Shutdown MRE from MainThread</param>
        protected AEWorkerThread(string threadName, AEServerMainThread mainThread, AEServerConfig state, ILogger logger, ManualResetEvent shutdownMRE)
        {
            _threadName = threadName;
            MainThread = mainThread;
            State = state;
            Logger = logger;
            ShutdownMRE = shutdownMRE;
        }

        /// <summary>Starts the worker thread</summary>
        /// <param name="preThreadStart">Action that needs performed before Thread.Start()</param>
        public virtual void Start(Action preThreadStart = null)
        {
            Logger.LogInfo($"{ThreadName} Starting", ThreadName);
            ThreadStatus = AEWorkerThreadStatus.Starting;

            WorkerThread = new Thread(() => ThreadLoop())
            {
                Name = ThreadName,
                IsBackground = true
            };

            preThreadStart?.Invoke();

            WorkerThread.Start();
        }

        /// <summary>Stops the worker thread.</summary>
        public virtual void Stop()
        {
            Logger.LogInfo($"{ThreadName} Shutting Down", ThreadName);
            Shutdown = true;
            ThreadStatus = AEWorkerThreadStatus.Stopping;

            WorkerThread.Join();

            ShutdownMRE.Set();
        }

        /// <summary>The actual loop for the worker thread.</summary>
        protected abstract void ThreadLoop();
    }
}
