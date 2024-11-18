using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;

namespace AutoEncodeUtilities.Communication.Data;

/// <summary>Data needed to describe an encoding job queue update.</summary>
public record EncodingJobQueueUpdateData()
{
    public EncodingJobQueueUpdateType Type { get; set; }

    public ulong JobId { get; set; }

    public EncodingJobData EncodingJob { get; set; }
}
