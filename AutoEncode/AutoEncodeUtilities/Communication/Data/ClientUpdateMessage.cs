using AutoEncodeUtilities.Communication.Enums;
using System.Text.Json;

namespace AutoEncodeUtilities.Communication.Data;

/// <summary>Basic class for containing client update data.</summary>
public class ClientUpdateMessage
{
    /// <summary>The type of update. </summary>
    public ClientUpdateType Type { get; set; }

    /// <summary>Update payload -- should be serialized to the correct type.</summary>
    public object Data { get; set; }

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
