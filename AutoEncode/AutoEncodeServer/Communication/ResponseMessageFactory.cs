using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeServer.Communication;

public static class ResponseMessageFactory
{
    public static CommunicationMessage<ResponseMessageType> CreateSourceFilesResponse(Dictionary<string, IEnumerable<SourceFileData>> sourceFiles)
        => new(ResponseMessageType.SourceFilesResponse, sourceFiles);

    public static CommunicationMessage<ResponseMessageType> CreateCancelResponse(bool success)
        => new(ResponseMessageType.CancelResponse, success);

    public static CommunicationMessage<ResponseMessageType> CreatePauseResponse(bool success)
        => new(ResponseMessageType.PauseResponse, success);

    public static CommunicationMessage<ResponseMessageType> CreateResumeResponse(bool success)
        => new(ResponseMessageType.ResumeResponse, success);

    public static CommunicationMessage<ResponseMessageType> CreatePauseAndCancelResponse(bool success)
        => new(ResponseMessageType.PauseCancelResponse, success);

    public static CommunicationMessage<ResponseMessageType> CreateEncodeResponse(bool success)
        => new(ResponseMessageType.EncodeResponse, success);

    public static CommunicationMessage<ResponseMessageType> CreateBulkEncodeResponse(IEnumerable<string> failedRequests)
        => new(ResponseMessageType.BulkEncodeResponse, failedRequests);

    public static CommunicationMessage<ResponseMessageType> CreateRemoveJobResponse(bool success)
        => new(ResponseMessageType.RemoveJobResponse, success);

    public static CommunicationMessage<ResponseMessageType> CreateJobQueueResponse(IEnumerable<EncodingJobData> queue)
        => new(ResponseMessageType.JobQueueResponse, queue);
}
