using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoEncodeUtilities.Json;

public class BoolJsonConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.Number => reader.TryGetInt32(out int value) && Convert.ToBoolean(value),
            JsonTokenType.String => bool.TryParse(reader.GetString(), out bool boolFromString) ? boolFromString : throw new JsonException("Unable to convert string to bool"),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            _ => throw new JsonException($"JsonTokenType {reader.TokenType} not implemented in {nameof(BoolJsonConverter)}.")
        };

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value);
}
