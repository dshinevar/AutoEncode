using AutomatedFFmpegServer.Base;
using AutomatedFFmpegUtilities.Config;

namespace AutomatedFFmpegServer.WorkerThreads
{
    public class EncodingThread : AFWorkerThreadBase
    {
        private bool Shutdown { get; set; } = false;

        public EncodingThread(AFServerMainThread mainThread, AFServerConfig serverConfig, EncodingJobs encodingJobs)
            : base("EncodingThread", mainThread, serverConfig, encodingJobs) { }

        public override void Stop()
        {
            Shutdown = true;
            base.Stop();
        }

        protected override void ThreadLoop(EncodingJobs encodingJobs, object[] objects = null)
        {
            while (Shutdown == false)
            {
                DeepSleep();
            }
        }
    }
}
