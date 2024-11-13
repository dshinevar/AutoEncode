using AutoEncodeUtilities.Communication.Enums;
using System.Text.Json;

namespace AutoEncodeUtilities.Communication.Data;

/// <summary>
/// Basic class for containing communication information between Server and Client.<br/>
/// Data payload is a generic object for JSON serialization.<br/>
/// </summary>
/// <param name="messageType">Indicates the type of request/response the message is.</param>
/// <param name="data">Optional data payload.</param>
/// <param name="message">Optional message to send -- usually used for display / logging.</param>
public sealed class CommunicationMessage(CommunicationMessageType messageType, object data = null, string message = "")
{
    public CommunicationMessageType MessageType { get; } = messageType;

    public object Data { get; } = data;

    public string Message { get; } = message;

    /// <summary>
    /// Converts a <see cref="CommunicationMessage"/> to <see cref="ConvertedMessage{TData}"/>.<br/>
    /// Deserializes the data payload of a <see cref="CommunicationMessage"/> based off the given data type.
    /// </summary>
    /// <typeparam name="TData">Expected data type.</typeparam>
    /// <param name="message">The <see cref="CommunicationMessage"/> to convert.</param>
    /// <returns><see cref="ConvertedMessage{TData}"/> with deserialized data payload (or default value).</returns>
    public static ConvertedMessage<TData> Convert<TData>(CommunicationMessage message)
    {
        TData convertedData = default;
        if (message.Data is JsonElement element)
        {
            try
            {
                convertedData = element.Deserialize<TData>(CommunicationConstants.SerializerOptions);
            }
            catch (JsonException) { }   // Just prevent a crash if a JsonException            
        }

        return new(message.MessageType, convertedData, message.Message);
    }
}


/// <summary>
/// Class converted from <see cref="CommunicationMessage"/> whose data payload was deserialized.<br/>
/// Not intended to be created directly. Use <see cref="CommunicationMessage.Convert{TData}(CommunicationMessage)"/>
/// </summary>
/// <typeparam name="TData">The data payload type.</typeparam>
/// <param name="messageType">Indicates the type of request/response the message is.</param>
/// <param name="data">Data payload.</param>
/// <param name="message">Optional message to send -- usually used for display / logging.</param>
public sealed class ConvertedMessage<TData>(CommunicationMessageType messageType, TData data, string message = "")
{
    public CommunicationMessageType MessageType { get; } = messageType;

    public TData Data { get; } = data;

    public string Message { get; set; } = message;
}


