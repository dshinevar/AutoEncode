using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using System;
using System.Collections.Generic;

namespace AutoEncodeClient.Communication;

public static class RequestMessageFactory
{
    public static CommunicationMessage<RequestMessageType> CreateSourceFilesRequest() => new(RequestMessageType.SourceFilesRequest);

    public static CommunicationMessage<RequestMessageType> CreateCancelJobRequest(ulong jobId) => new(RequestMessageType.CancelRequest, jobId);

    public static CommunicationMessage<RequestMessageType> CreatePauseJobRequest(ulong jobId) => new(RequestMessageType.PauseRequest, jobId);

    public static CommunicationMessage<RequestMessageType> CreateResumeJobRequest(ulong jobId) => new(RequestMessageType.ResumeRequest, jobId);

    public static CommunicationMessage<RequestMessageType> CreatePauseAndCancelRequest(ulong jobId) => new(RequestMessageType.PauseCancelRequest, jobId);

    public static CommunicationMessage<RequestMessageType> CreateRemoveJobRequest(ulong jobId) => new(RequestMessageType.RemoveJobRequest, jobId);

    public static CommunicationMessage<RequestMessageType> CreateEncodeRequest(Guid sourceFileGuid) => new(RequestMessageType.EncodeRequest, sourceFileGuid);

    public static CommunicationMessage<RequestMessageType> CreateBulkEncodeRequest(IEnumerable<Guid> sourceFileGuids) => new(RequestMessageType.BulkEncodeRequest, sourceFileGuids);

    public static CommunicationMessage<RequestMessageType> CreateJobQueueRequest() => new(RequestMessageType.JobQueueRequest);
}
