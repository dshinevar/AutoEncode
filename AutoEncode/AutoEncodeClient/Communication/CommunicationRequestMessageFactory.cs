using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using System;
using System.Collections.Generic;

namespace AutoEncodeClient.Communication;

public static class CommunicationRequestMessageFactory
{
    public static CommunicationMessage CreateSourceFilesRequest() => new(CommunicationMessageType.SourceFilesRequest);

    public static CommunicationMessage CreateCancelJobRequest(ulong jobId) => new(CommunicationMessageType.CancelRequest, jobId);

    public static CommunicationMessage CreatePauseJobRequest(ulong jobId) => new(CommunicationMessageType.PauseRequest, jobId);

    public static CommunicationMessage CreateResumeJobRequest(ulong jobId) => new(CommunicationMessageType.ResumeRequest, jobId);

    public static CommunicationMessage CreatePauseAndCancelRequest(ulong jobId) => new(CommunicationMessageType.PauseCancelRequest, jobId);

    public static CommunicationMessage CreateEncodeRequest(Guid sourceFileGuid) => new(CommunicationMessageType.EncodeRequest, sourceFileGuid);

    public static CommunicationMessage CreateBulkEncodeRequest(IEnumerable<Guid> sourceFileGuids) => new(CommunicationMessageType.BulkEncodeRequest, sourceFileGuids);

    public static CommunicationMessage CreateRemoveJobRequest(ulong jobId) => new(CommunicationMessageType.RemoveJobRequest, jobId);

    public static CommunicationMessage CreateJobQueueRequest() => new(CommunicationMessageType.JobQueueRequest);
}
