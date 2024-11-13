using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;

namespace AutoEncodeUtilities.Communication.Data;

public class EncodingJobQueueUpdateData()
{
    public EncodingJobQueueUpdateType Type { get; set; }

    public ulong JobId { get; set; }

    public EncodingJobData EncodingJob { get; set; }
}
