using AutoEncodeUtilities.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace AutoEncodeUtilities.Messages
{
    public class AEMessage
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public AEMessageType MessageType { get; }

        public AEMessage(AEMessageType messageType)
        {
            MessageType = messageType;
        }
    }

    public class AEMessage<T> : AEMessage 
    {
        public AEMessage(AEMessageType messageType, T data)
            : base(messageType)
        {
            Data = data;
        }

        public T Data { get; }
    }
}
