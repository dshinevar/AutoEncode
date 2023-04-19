using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Messages
{
    public static class AEMessageFactory
    {
        public static AEMessage CreateEncodingJobQueueRequest()
            => new(AEMessageType.Status_Queue_Request);
        public static AEMessage<List<EncodingJobData>> CreateEncodingJobQueueResponse(List<EncodingJobData> queue, Guid guid)
            => new(AEMessageType.Status_Queue_Response, queue, guid);

        public static AEMessage CreateMovieSourceFilesRequest()
            => new(AEMessageType.Status_MovieSourceFiles_Request);
        public static AEMessage<Dictionary<string, List<VideoSourceData>>> CreateMovieSourceFilesResponse(Dictionary<string, List<VideoSourceData>> files, Guid guid)
            => new(AEMessageType.Status_MovieSourceFiles_Response, files, guid);

        public static AEMessage CreateShowSourceFilesRequest()
            => new(AEMessageType.Status_ShowSourceFiles_Request);
        public static AEMessage<Dictionary<string, List<ShowSourceData>>> CreateShowSourceFilesResponse(Dictionary<string, List<ShowSourceData>> files, Guid guid)
            => new(AEMessageType.Status_ShowSourceFiles_Response, files, guid);
    }
}
