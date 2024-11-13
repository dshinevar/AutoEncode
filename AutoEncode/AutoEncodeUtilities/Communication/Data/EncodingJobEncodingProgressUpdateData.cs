using System;

namespace AutoEncodeUtilities.Communication.Data;

public class EncodingJobEncodingProgressUpdateData
{
    public byte EncodingProgress { get; set; }

    public double? CurrentFramesPerSecond { get; set; }

    public TimeSpan? EstimatedEncodingTimeRemaining { get; set; }

    public TimeSpan ElapsedEncodingTime { get; set; }

    public DateTime? CompletedEncodingDateTime { get; set; }

    public DateTime? CompletedPostProcessingTime { get; set; }
}