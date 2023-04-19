using AutoEncodeUtilities.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace AutoEncodeUtilities.Messages
{
    public class AEMessage
    {
        public Guid Guid { get; }
        public bool IsResponse { get; } = false;
        public bool IsRequest { get; } = false;

        [JsonConverter(typeof(StringEnumConverter))]
        public AEMessageType MessageType { get; }

        public AEMessage(AEMessageType messageType, Guid guid = default)
        {
            MessageType = messageType;

            if (guid == Guid.Empty)
            {
                Guid = Guid.NewGuid();
                IsRequest = true;
            }
            else
            {
                Guid = guid;
                IsResponse = true;
            }
        }
    }

    public class AEMessage<T> : AEMessage 
    {
        public AEMessage(AEMessageType messageType, T data, Guid guid = default)
            : base(messageType, guid)
        {
            Data = data;
        }

        public T Data { get; }
    }
}
