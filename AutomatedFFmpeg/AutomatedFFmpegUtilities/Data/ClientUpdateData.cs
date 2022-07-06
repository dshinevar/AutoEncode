using System.Collections.Generic;

namespace AutomatedFFmpegUtilities.Data
{
    public class ClientUpdateData
    {
        public List<ThreadStatusData> ThreadStatuses { get; set; }
        public List<EncodingJobClientData> EncodingJobs { get; set; }
    }
}
