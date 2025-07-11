using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeServer.Communication;

public static class ClientUpdateMessageFactory
{
    public static (string, CommunicationMessage<ClientUpdateType>) CreateSourceFileUpdate(IEnumerable<SourceFileUpdateData> data)
    {
        string topic = nameof(ClientUpdateType.SourceFilesUpdate);
        CommunicationMessage<ClientUpdateType> message = new(ClientUpdateType.SourceFilesUpdate, data);
        return (topic, message);
    }

    public static (string, CommunicationMessage<ClientUpdateType>) CreateEncodingJobStatusUpdate(ulong jobId, EncodingJobStatusUpdateData data)
    {
        string topic = $"{jobId}-{nameof(ClientUpdateType.EncodingJobStatus)}";
        CommunicationMessage<ClientUpdateType> message = new(ClientUpdateType.EncodingJobStatus, data);
        return (topic, message);
    }

    public static (string, CommunicationMessage<ClientUpdateType>) CreateEncodingJobProcessingDataUpdate(ulong jobId, EncodingJobProcessingDataUpdateData data)
    {
        string topic = $"{jobId}-{nameof(ClientUpdateType.EncodingJobProcessingData)}";
        CommunicationMessage<ClientUpdateType> message = new(ClientUpdateType.EncodingJobProcessingData, data);
        return (topic, message);
    }

    public static (string, CommunicationMessage<ClientUpdateType>) CreateEncodingJobEncodingProgressUpdate(ulong jobId, EncodingJobEncodingProgressUpdateData data)
    {
        string topic = $"{jobId}-{nameof(ClientUpdateType.EncodingJobEncodingProgress)}";
        CommunicationMessage<ClientUpdateType> message = new(ClientUpdateType.EncodingJobEncodingProgress, data);
        return (topic, message);
    }

    public static (string, CommunicationMessage<ClientUpdateType>) CreateEncodingJobQueueUpdate(EncodingJobQueueUpdateType type, ulong jobId, EncodingJobData data = null)
    {
        string topic = nameof(ClientUpdateType.EncodingJobQueue);
        CommunicationMessage<ClientUpdateType> message = new(ClientUpdateType.EncodingJobQueue, new EncodingJobQueueUpdateData()
        {
            Type = type,
            JobId = jobId,
            EncodingJob = data
        });
        return (topic, message);
    }
}
