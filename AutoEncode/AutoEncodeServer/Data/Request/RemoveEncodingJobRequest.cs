using AutoEncodeServer.Enums;

namespace AutoEncodeServer.Data.Request;

/// <summary>Request data object for removing an encoding job. </summary>
internal class RemoveEncodingJobRequest
{
    public ulong JobId { get; set; }

    public RemovedEncodingJobReason Reason { get; set; }
}
