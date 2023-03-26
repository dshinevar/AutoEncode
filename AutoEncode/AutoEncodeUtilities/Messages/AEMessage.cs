using AutoEncodeUtilities.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AutoEncodeUtilities.Messages
{
    public class AEMessage
    {
        public AEMessage(AEMessageType messageType)
        {
            MessageType = messageType;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public AEMessageType MessageType { get; set; }
    }

    public class AEMessage<T> : AEMessage
    {
        public AEMessage(AEMessageType messageType, T data)
            : base(messageType)
        {
            Data = data;
        }

        public T Data { get; set; }
    }
}
