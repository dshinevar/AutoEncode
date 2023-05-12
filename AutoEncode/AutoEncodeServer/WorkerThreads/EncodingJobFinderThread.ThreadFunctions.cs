using AutoEncodeUtilities.Enums;
using System.Threading;

namespace AutoEncodeServer.WorkerThreads
{
    public partial class EncodingJobFinderThread
    {
        #region Shutdown Properties
        /// <summary>Shutdown MRE from MainThread; Signals to MainThread that Shutdown is complete.</summary>
        private ManualResetEvent ShutdownMRE { get; set; }
        /// <summary>Cancellation Token used for shutting down the thread.</summary>
        private CancellationTokenSource ShutdownCancellationTokenSource { get; set; } = new CancellationTokenSource();
        #endregion Shutdown Properties

        #region Thread Properties
        /// <summary><see cref="Thread"/> Object</summary>
        private Thread WorkerThread { get; set; }
        /// <summary>Name of the Worker Thread</summary>
        protected string ThreadName => WorkerThread?.Name ?? nameof(EncodingJobFinderThread);
        /// <summary>The status of the Worker Thread</summary>
        protected AEWorkerThreadStatus ThreadStatus { get; set; } = AEWorkerThreadStatus.Processing;
        #endregion Thread Properties

        public void Start()
        {
            Logger.LogInfo($"{ThreadName} Starting", ThreadName);
            ThreadStatus = AEWorkerThreadStatus.Starting;

            WorkerThread = new Thread(ThreadLoop)
            {
                Name = ThreadName,
                IsBackground = true
            };

            BuildSourceFiles();

            WorkerThread.Start(ShutdownCancellationTokenSource.Token);
        }

        public void Stop()
        {
            Logger.LogInfo($"{ThreadName} Shutting Down", ThreadName);

            ShutdownCancellationTokenSource.Cancel();
            ThreadStatus = AEWorkerThreadStatus.Stopping;
            Wake();

            WorkerThread.Join();

            ShutdownMRE.Set();
        }

        /// <summary> Wakes up thread by setting the Sleep AutoResetEvent.</summary>
        public void Wake() => SleepARE.Set();

        /// <summary> Sleeps thread for certain amount of time. </summary>
        private void Sleep()
        {
            if (ShutdownCancellationTokenSource.IsCancellationRequested is false)
            {
                ThreadStatus = AEWorkerThreadStatus.Sleeping;
                SleepARE.WaitOne(ThreadSleep);
            }
        }
    }
}
