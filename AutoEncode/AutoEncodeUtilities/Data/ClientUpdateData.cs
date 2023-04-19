using System.Collections.Generic;

namespace AutoEncodeUtilities.Data
{
    public class ClientUpdateData
    {
        public List<ThreadStatusData> ThreadStatuses { get; set; }
        public List<EncodingJobData> EncodingJobs { get; set; }
    }
}
