using System.Collections.Generic;

namespace AutoEncodeUtilities.Data
{
    public class ClientUpdateData
    {
        public List<ThreadStatusData> ThreadStatuses { get; set; }
        public List<EncodingJobClientData> EncodingJobs { get; set; }
    }
}
