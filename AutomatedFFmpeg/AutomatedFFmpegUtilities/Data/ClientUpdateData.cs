using System.Collections.Generic;

namespace AutomatedFFmpegUtilities.Data
{
    public class ClientUpdateData
    {
        public List<ThreadStatusData> ThreadStatuses { get; set; }
        public List<EncodingJob> EncodingJobs { get; set; }
    }
}
