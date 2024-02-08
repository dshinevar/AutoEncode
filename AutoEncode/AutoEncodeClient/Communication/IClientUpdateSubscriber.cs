using AutoEncodeClient.Data;
using AutoEncodeUtilities.Data;
using System;
using System.Collections.Generic;

namespace AutoEncodeClient.Communication
{
    public interface IClientUpdateSubscriber
    {
        string ConnectionString { get; }

        string IpAddress { get; }

        int Port { get; }

        event EventHandler<EncodingJobStatusUpdateData> StatusUpdateReceived;

        event EventHandler<EncodingJobProcessingDataUpdateData> ProcessingDataUpdateReceived;

        event EventHandler<EncodingJobEncodingProgressUpdateData> EncodingProgressUpdateReceived;

        event EventHandler<IEnumerable<EncodingJobData>> QueueUpdateReceived;

        void Initialize(string ipAddress, int port, IEnumerable<SubscriberTopic> topics);

        void Start();

        void Stop();
    }
}
