using System;
using System.Text.Json;

namespace AutoEncodeUtilities.Communication.Data;

/// <summary>Basic communication class. Used for request/response as well as update streams. </summary>
/// <typeparam name="TMessageType">The type of communication message (request, response, update, etc.)</typeparam>
/// <param name="Type">The specific message type.</param>
/// <param name="Data"></param>
/// <param name="Message"></param>
public sealed record CommunicationMessage<TMessageType>(TMessageType Type, object Data = null, string Message = "")
    where TMessageType : Enum
{
    /// <summary>Attempts to unpack / deserialize <see cref="Data"/> into the given type. </summary>
    /// <typeparam name="TData">Type to deserialize into.</typeparam>
    /// <returns>Unpacked data -- default value if fails.</returns>
    public TData UnpackData<TData>()
    {
        TData unpackedData = default;
        if (Data is JsonElement element)
        {
            try
            {
                unpackedData = element.Deserialize<TData>(CommunicationConstants.SerializerOptions);
            }
            catch (JsonException) { }   // Just prevent a crash if a JsonException            
        }

        return unpackedData;
    }
}
