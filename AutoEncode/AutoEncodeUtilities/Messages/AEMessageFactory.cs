using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Messages
{
    public static class AEMessageFactory
    {
        public static AEMessage CreateMovieSourceFilesRequest()
            => new(AEMessageType.Status_MovieSourceFiles_Request);

        public static AEMessage<Dictionary<string, List<VideoSourceData>>> CreateMovieSourceFilesResponse(Dictionary<string, List<VideoSourceData>> files)
            => new(AEMessageType.Status_MovieSourceFiles_Response, files);

        public static AEMessage CreateShowSourceFilesRequest()
            => new(AEMessageType.Status_ShowSourceFiles_Request);

        public static AEMessage<Dictionary<string, List<ShowSourceData>>> CreateShowSourceFilesResponse(Dictionary<string, List<ShowSourceData>> files)
            => new(AEMessageType.Status_ShowSourceFiles_Response, files);

        public static AEMessage<ulong> CreateCancelRequest(ulong jobId) => new(AEMessageType.Cancel_Request, jobId);

        public static AEMessage<bool> CreateCancelResponse(bool success) => new(AEMessageType.Cancel_Request, success);

        public static AEMessage<ulong> CreatePauseRequest(ulong jobId) => new(AEMessageType.Pause_Request, jobId);

        public static AEMessage<bool> CreatePauseResponse(bool success) => new(AEMessageType.Pause_Response, success);

        public static AEMessage<ulong> CreateResumeRequest(ulong jobId) => new(AEMessageType.Resume_Request, jobId);

        public static AEMessage<bool> CreateResumeResponse(bool success) => new(AEMessageType.Resume_Response, success);

        public static AEMessage<ulong> CreateCancelPauseRequest(ulong jobId) => new(AEMessageType.Cancel_Pause_Request, jobId);

        public static AEMessage<bool> CreateCancelPauseResponse(bool success) => new(AEMessageType.Cancel_Pause_Response, success);
    }
}
