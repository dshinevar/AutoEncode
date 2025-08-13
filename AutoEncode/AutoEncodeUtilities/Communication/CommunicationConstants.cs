using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoEncodeUtilities.Communication;

public static class CommunicationConstants
{
    static CommunicationConstants()
    {
        SerializerOptions = new()
        {
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
    }

    public static readonly JsonSerializerOptions SerializerOptions;
}
