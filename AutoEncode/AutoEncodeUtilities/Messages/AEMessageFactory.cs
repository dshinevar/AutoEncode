using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Messages
{
    public static class AEMessageFactory
    {
        public static AEMessage CreateSourceFilesRequest() => new(AEMessageType.Source_Files_Request);

        public static AEMessage<SourceFilesResponse> CreateSourceFilesResponse(IDictionary<string, IEnumerable<SourceFileData>> movies, IDictionary<string, IEnumerable<ShowSourceFileData>> shows)
            => new(AEMessageType.Source_Files_Response, new()
            {
                MovieSourceFiles = movies,
                ShowSourceFiles = shows
            });

        public static AEMessage<ulong> CreateCancelRequest(ulong jobId) => new(AEMessageType.Cancel_Request, jobId);

        public static AEMessage<bool> CreateCancelResponse(bool success) => new(AEMessageType.Cancel_Request, success);

        public static AEMessage<ulong> CreatePauseRequest(ulong jobId) => new(AEMessageType.Pause_Request, jobId);

        public static AEMessage<bool> CreatePauseResponse(bool success) => new(AEMessageType.Pause_Response, success);

        public static AEMessage<ulong> CreateResumeRequest(ulong jobId) => new(AEMessageType.Resume_Request, jobId);

        public static AEMessage<bool> CreateResumeResponse(bool success) => new(AEMessageType.Resume_Response, success);

        public static AEMessage<ulong> CreateCancelPauseRequest(ulong jobId) => new(AEMessageType.Cancel_Pause_Request, jobId);

        public static AEMessage<bool> CreateCancelPauseResponse(bool success) => new(AEMessageType.Cancel_Pause_Response, success);

        public static AEMessage<EncodeRequest> CreateEncodeRequest(Guid guid, bool isShow) =>
            new(AEMessageType.Encode_Request, new EncodeRequest()
            {
                Guid = guid,
                IsShow = isShow
            });

        public static AEMessage<bool> CreateEncodeResponse(bool success) => new(AEMessageType.Encode_Response, success);
    }
}
