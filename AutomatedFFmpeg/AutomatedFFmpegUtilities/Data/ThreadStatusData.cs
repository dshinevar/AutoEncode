using AutomatedFFmpegUtilities.Enums;

namespace AutomatedFFmpegUtilities.Data
{
    public class ThreadStatusData
    {
        public string ThreadName { get; set; }
        public AFWorkerThreadStatus ThreadStatus { get; set; }

        public ThreadStatusData(string threadName, AFWorkerThreadStatus threadStatus)
        {
            ThreadName = threadName;
            ThreadStatus = threadStatus;
        }
    }
}
