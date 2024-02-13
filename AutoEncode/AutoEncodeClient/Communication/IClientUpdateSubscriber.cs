using AutoEncodeUtilities.Data;
using System;

namespace AutoEncodeClient.Communication
{
    public interface IClientUpdateSubscriber
    {
        string ConnectionString { get; }

        string IpAddress { get; }

        int Port { get; }

        void Initialize(string ipAddress, int port);

        void Start();

        void Stop();

        bool SubscribeToEncodingJobQueueUpdate(string topic, Action<EncodingJobQueueUpdateData> dataReceivedCallback);

        bool SubscribeToEncodingJobStatusUpdate(string topic, Action<EncodingJobStatusUpdateData> dataReceivedCallback);

        bool SubscribeToEncodingJobProcessingDataUpdate(string topic, Action<EncodingJobProcessingDataUpdateData> dataReceivedCallback);

        bool SubscribeToEncodingJobEncodingProgressUpdate(string topic, Action<EncodingJobEncodingProgressUpdateData> dataReceivedCallback);

        bool Unsubscribe(string topic);
    }
}
