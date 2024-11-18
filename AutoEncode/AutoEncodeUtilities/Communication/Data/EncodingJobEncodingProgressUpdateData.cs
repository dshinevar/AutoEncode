using System;

namespace AutoEncodeUtilities.Communication.Data;

/// <summary>Encapsulates data describing an encoding job's encoding progress.</summary>
public record EncodingJobEncodingProgressUpdateData
{
    public byte EncodingProgress { get; set; }

    public double? CurrentFramesPerSecond { get; set; }

    public TimeSpan? EstimatedEncodingTimeRemaining { get; set; }

    public TimeSpan ElapsedEncodingTime { get; set; }

    public DateTime? CompletedEncodingDateTime { get; set; }

    public DateTime? CompletedPostProcessingTime { get; set; }
}