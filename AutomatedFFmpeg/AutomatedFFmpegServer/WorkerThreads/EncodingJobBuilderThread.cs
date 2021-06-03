using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using AutomatedFFmpegServer.Base;
using AutomatedFFmpegUtilities.Config;


namespace AutomatedFFmpegServer.WorkerThreads
{
    public class EncodingJobBuilderThread : AFWorkerThreadBase
    {
        private bool _shutdown { get; set; } = false;
        public EncodingJobBuilderThread(AFServerMainThread mainThread, AFServerConfig serverConfig, EncodingJobs encodingJobs) 
            : base("EncodingJobBuilderThread", mainThread, serverConfig, encodingJobs) { }

        public override void Shutdown()
        {
            _shutdown = true;
            base.Shutdown();
        }

        protected override void ThreadLoop(EncodingJobs encodingJobs, object[] objects = null)
        {
            while (_shutdown == false)
            {
                Thread.Sleep(5000);
            }
        }

    }
}
