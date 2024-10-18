using Newtonsoft.Json;

namespace AutoEncodeUtilities;

public static class CommunicationConstants
{
    public static readonly JsonSerializerSettings SerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };

    public const string EncodingJobStatusUpdate = "STATUS";

    public const string EncodingJobProcessingDataUpdate = "PROCESSING_DATA";

    public const string EncodingJobEncodingProgressUpdate = "ENCODING_PROGRESS";

    public const string EncodingJobQueueUpdate = "QUEUE_UPDATE";
}
