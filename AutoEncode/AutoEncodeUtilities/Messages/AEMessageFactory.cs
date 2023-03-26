using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Messages
{
    public static class AEMessageFactory
    {
        public static AEMessage CreateEncodingJobQueueRequest()
            => new(AEMessageType.Status_Queue_Request);
        public static AEMessage<List<EncodingJobData>> CreateEncodingJobQueueResponse(List<EncodingJobData> queue)
            => new(AEMessageType.Status_Queue_Response, queue);
    }
}
