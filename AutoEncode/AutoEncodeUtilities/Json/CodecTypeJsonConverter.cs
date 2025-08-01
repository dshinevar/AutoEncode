using AutoEncodeUtilities.Enums;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoEncodeUtilities.Json;

public class CodecTypeJsonConverter : JsonConverter<CodecType>
{
    public override CodecType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString().Trim().ToLower() switch
        {
            "video" => CodecType.Video,
            "audio" => CodecType.Audio,
            "subtitle" => CodecType.Subtitle,
            _ => CodecType.Unknown
        };

    public override void Write(Utf8JsonWriter writer, CodecType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.GetShortName());
}
