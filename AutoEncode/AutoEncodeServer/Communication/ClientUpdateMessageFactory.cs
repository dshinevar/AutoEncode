using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeServer.Communication;

public static class ClientUpdateMessageFactory
{
    public static (string, ClientUpdateMessage) CreateSourceFileUpdate(IEnumerable<SourceFileUpdateData> data)
    {
        string topic = nameof(ClientUpdateType.SourceFilesUpdate);
        ClientUpdateMessage message = new()
        {
            Type = ClientUpdateType.SourceFilesUpdate,
            Data = data
        };

        return (topic, message);
    }

    public static (string, ClientUpdateMessage) CreateEncodingJobStatusUpdate(ulong jobId, EncodingJobStatusUpdateData data)
    {
        string topic = $"{nameof(ClientUpdateType.EncodingJobStatus)}-{jobId}";
        ClientUpdateMessage message = new()
        {
            Type = ClientUpdateType.EncodingJobStatus,
            Data = data
        };

        return (topic, message);
    }

    public static (string, ClientUpdateMessage) CreateEncodingJobProcessingDataUpdate(ulong jobId, EncodingJobProcessingDataUpdateData data)
    {
        string topic = $"{nameof(ClientUpdateType.EncodingJobProcessingData)}-{jobId}";
        ClientUpdateMessage message = new()
        {
            Type = ClientUpdateType.EncodingJobProcessingData,
            Data = data
        };

        return (topic, message);
    }

    public static (string, ClientUpdateMessage) CreateEncodingJobEncodingProgressUpdate(ulong jobId, EncodingJobEncodingProgressUpdateData data)
    {
        string topic = $"{nameof(ClientUpdateType.EncodingJobEncodingProgress)}-{jobId}";
        ClientUpdateMessage message = new()
        {
            Type = ClientUpdateType.EncodingJobEncodingProgress,
            Data = data
        };

        return (topic, message);
    }

    public static (string, ClientUpdateMessage) CreateEncodingJobQueueUpdate(EncodingJobQueueUpdateType type, ulong jobId, EncodingJobData data = null)
    {
        string topic = nameof(ClientUpdateType.EncodingJobQueue);
        ClientUpdateMessage message = new()
        {
            Type = ClientUpdateType.EncodingJobQueue,
            Data = new EncodingJobQueueUpdateData()
            {
                Type = type,
                JobId = jobId,
                EncodingJob = data
            }
        };

        return (topic, message);
    }
}
