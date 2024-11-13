using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Data.Response;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeServer.Communication;

public static class CommunicationResponseMessageFactory
{
    public static CommunicationMessage CreateSourceFilesResponse(Dictionary<string, IEnumerable<SourceFileData>> sourceFiles)
        => new(CommunicationMessageType.SourceFilesResponse, new SourceFilesResponse()
        {
            SourceFiles = sourceFiles
        });

    public static CommunicationMessage CreateCancelResponse(bool success)
        => new(CommunicationMessageType.CancelResponse, success);

    public static CommunicationMessage CreatePauseResponse(bool success)
        => new(CommunicationMessageType.PauseResponse, success);

    public static CommunicationMessage CreateResumeResponse(bool success)
        => new(CommunicationMessageType.ResumeResponse, success);

    public static CommunicationMessage CreatePauseAndCancelResponse(bool success)
        => new(CommunicationMessageType.PauseCancelResponse, success);

    public static CommunicationMessage CreateEncodeResponse(bool success)
        => new(CommunicationMessageType.EncodeResponse, success);

    public static CommunicationMessage CreateBulkEncodeResponse(IEnumerable<string> failedRequests)
        => new(CommunicationMessageType.BulkEncodeResponse, failedRequests);

    public static CommunicationMessage CreateRemoveJobResponse(bool success)
        => new(CommunicationMessageType.RemoveJobResponse, success);

    public static CommunicationMessage CreateJobQueueResponse(IEnumerable<EncodingJobData> queue)
        => new(CommunicationMessageType.JobQueueResponse, queue);
}
