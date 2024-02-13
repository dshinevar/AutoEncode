using AutoEncodeUtilities.Interfaces;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Data
{
    public class EncodingJobQueueUpdateData(IEnumerable<EncodingJobData> queue) : IClientUpdateData
    {
        public IEnumerable<EncodingJobData> Queue { get; set; } = queue;
    }
}
