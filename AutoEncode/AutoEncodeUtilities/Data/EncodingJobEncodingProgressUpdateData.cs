using System;

namespace AutoEncodeUtilities.Data
{
    public class EncodingJobEncodingProgressUpdateData
    {
        public byte EncodingProgress { get; set; }

        public TimeSpan ElapsedEncodingTime { get; set; }

        public DateTime? CompletedEncodingDateTime { get; set; }

        public DateTime? CompletedPostProcessingTime { get; set; }
    }
}
