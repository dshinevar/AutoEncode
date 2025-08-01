using AutoEncodeUtilities.Enums;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoEncodeUtilities.Json;

public class ChromaLocationJsonConverter : JsonConverter<ChromaLocation?>
{
    public override ChromaLocation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString().Trim().ToLower() switch
        {
            "topleft" => ChromaLocation.TOP_LEFT,
            "center" => ChromaLocation.CENTER,
            "top" => ChromaLocation.TOP,
            "bottomleft" => ChromaLocation.BOTTOM_LEFT,
            "bottom" => ChromaLocation.BOTTOM,
            "left" => ChromaLocation.LEFT_DEFAULT,
            _ => null,
        };

    public override void Write(Utf8JsonWriter writer, ChromaLocation? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value?.GetShortName());
}
