using System.Threading;
using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Data;

namespace AutomatedFFmpegServer.Base
{
    /// <summary> Worker thread base class.  Uses a thread loop as the worker threads are expected to have lengthy tasks. </summary>
    public abstract class AFWorkerThreadBase
    {
        private Thread _thread { get; set; }
        protected AFServerMainThread MainThread { get; set; }
        protected EncodingJobs EncodingJobs { get; set; }
        protected AFServerConfig Config { get; set; }
        public string ThreadName { get; set; } = "AFWorkerThread";
        public AFWorkerThreadStatus Status { get; set; } = AFWorkerThreadStatus.PROCESSING;

        /// <summary>Constructor</summary>
        /// <param name="encodingJobs">EncodingJobs handle.</param>
        public AFWorkerThreadBase(string threadName, AFServerMainThread mainThread, AFServerConfig serverConfig, EncodingJobs encodingJobs)
        {
            ThreadName = threadName;
            MainThread = mainThread;
            Config = serverConfig;
            EncodingJobs = encodingJobs;
        }

        /// <summary>Gets the current status of the thread. </summary>
        /// <returns>ThreadStatusData</returns>
        public ThreadStatusData GetThreadStatus() => new ThreadStatusData(ThreadName, Status);

        /// <summary> Starts thread. </summary>
        public virtual void Start(params object[] threadObjects)
        {
            ThreadStart threadStart = () => ThreadLoop(EncodingJobs, threadObjects);
            _thread = new Thread(threadStart);
            _thread.Start();
        }
        /// <summary> Stops thread (join). </summary>
        public virtual void Shutdown() => _thread.Join();

        protected abstract void ThreadLoop(EncodingJobs encodingJobs, object[] objects = null);
    }
}
