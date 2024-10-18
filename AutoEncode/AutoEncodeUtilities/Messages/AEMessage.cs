using AutoEncodeUtilities.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AutoEncodeUtilities.Messages;

public class AEMessage(AEMessageType messageType)
{
    [JsonConverter(typeof(StringEnumConverter))]
    public AEMessageType MessageType { get; } = messageType;
}

public class AEMessage<T>(AEMessageType messageType, T data) : AEMessage(messageType)
{
    public T Data { get; } = data;
}
