using AutoEncodeClient.Enums;

namespace AutoEncodeClient.Data
{
    public class SubscriberTopic(ClientUpdateType clientUpdateType, string topic)
    {
        public ClientUpdateType ClientUpdateType { get; set; } = clientUpdateType;

        public string Topic { get; set; } = topic;
    }
}
