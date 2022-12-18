using AutoEncodeUtilities.Enums;

namespace AutoEncodeUtilities.Data
{
    public class ThreadStatusData
    {
        public string ThreadName { get; set; }
        public AEWorkerThreadStatus ThreadStatus { get; set; }

        public ThreadStatusData(string threadName, AEWorkerThreadStatus threadStatus)
        {
            ThreadName = threadName;
            ThreadStatus = threadStatus;
        }
    }
}
