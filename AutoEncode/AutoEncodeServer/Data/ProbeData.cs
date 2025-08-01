using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutoEncodeServer.Data;

/// <summary>Data object to be serialized from the json returned from ffprobe </summary>
public class ProbeData
{
    [JsonPropertyName("frames")]
    public List<Frame> Frames { get; set; }
    [JsonPropertyName("streams")]
    public List<Stream> Streams { get; set; }
    [JsonPropertyName("format")]
    public Format Format { get; set; }
}

public class Frame
{
    [JsonPropertyName("media_type")]
    public string MediaType { get; set; }
    [JsonPropertyName("stream_index")]
    public short StreamIndex { get; set; }
    [JsonPropertyName("pix_fmt")]
    public string PixelFormat { get; set; }
    [JsonPropertyName("color_range")]
    public string ColorRange { get; set; }
    [JsonPropertyName("color_space")]
    public string ColorSpace { get; set; }
    [JsonPropertyName("color_primaries")]
    public string ColorPrimaries { get; set; }
    [JsonPropertyName("color_transfer")]
    public string ColorTransfer { get; set; }
    [JsonConverter(typeof(ChromaLocationJsonConverter))]
    [JsonPropertyName("chroma_location")]
    public ChromaLocation? ChromaLocation { get; set; }
    [JsonPropertyName("side_data_list")]
    public List<SideData> SideData { get; set; }
}

public class SideData
{
    [JsonPropertyName("side_data_type")]
    public string SideDataType { get; set; }

    // SideDataType: Mastering display metadata
    [JsonPropertyName("red_x")]
    public string RedX { get; set; }
    [JsonPropertyName("red_y")]
    public string RedY { get; set; }
    [JsonPropertyName("green_x")]
    public string GreenX { get; set; }
    [JsonPropertyName("green_y")]
    public string GreenY { get; set; }
    [JsonPropertyName("blue_x")]
    public string BlueX { get; set; }
    [JsonPropertyName("blue_y")]
    public string BlueY { get; set; }
    [JsonPropertyName("white_point_x")]
    public string WhitePointX { get; set; }
    [JsonPropertyName("white_point_y")]
    public string WhitePointY { get; set; }
    [JsonPropertyName("min_luminance")]
    public string MinLuminance { get; set; }
    [JsonPropertyName("max_luminance")]
    public string MaxLuminance { get; set; }

    // SideDataType: Content light level metadata
    [JsonPropertyName("max_content")]
    public int MaxContent { get; set; }
    [JsonPropertyName("max_average")]
    public int MaxAverage { get; set; }

    // SideDataType: DOVI configuration record
    [JsonPropertyName("dv_version_major")]
    public int DolbyVisionVersionMajor { get; set; }
    [JsonPropertyName("dv_version_minor")]
    public int DolbyVisionVersionMinor { get; set; }
    [JsonPropertyName("dv_profile")]
    public int DolbyVisionProfile { get; set; }
    [JsonPropertyName("dv_level")]
    public int DolbyVisionLevel { get; set; }
    [JsonConverter(typeof(BoolJsonConverter))]
    [JsonPropertyName("rpu_present_flag")]
    public bool RPUPresent { get; set; }
    [JsonConverter(typeof(BoolJsonConverter))]
    [JsonPropertyName("el_present_flag")]
    public bool ELPresent { get; set; }
    [JsonConverter(typeof(BoolJsonConverter))]
    [JsonPropertyName("bl_present_flag")]
    public bool BLPresent { get; set; }
}

public class Stream
{
    [JsonPropertyName("tags")]
    public Tags Tags { get; set; }
    [JsonPropertyName("disposition")]
    public Disposition Disposition { get; set; }

    [JsonPropertyName("index")]
    public short Index { get; set; }
    [JsonConverter(typeof(CodecTypeJsonConverter))]
    [JsonPropertyName("codec_type")]
    public CodecType CodecType { get; set; }
    [JsonPropertyName("codec_name")]
    public string CodecName { get; set; }
    [JsonPropertyName("codec_long_name")]
    public string CodecLongName { get; set; }
    [JsonPropertyName("profile")]
    public string Profile { get; set; }

    // CodecType Video
    [JsonPropertyName("side_data_list")]
    public List<SideData> SideData { get; set; }
    [JsonPropertyName("width")]
    public int Width { get; set; }
    [JsonPropertyName("height")]
    public int Height { get; set; }
    [JsonPropertyName("pix_fmt")]
    public string PixelFormat { get; set; }
    [JsonPropertyName("color_range")]
    public string ColorRange { get; set; }
    [JsonPropertyName("color_space")]
    public string ColorSpace { get; set; }
    [JsonPropertyName("color_primaries")]
    public string ColorPrimaries { get; set; }
    [JsonPropertyName("color_transfer")]
    public string ColorTransfer { get; set; }
    [JsonConverter(typeof(ChromaLocationJsonConverter))]
    [JsonPropertyName("chroma_location")]
    public ChromaLocation? ChromaLocation { get; set; }
    [JsonPropertyName("has_b_frames")]
    public int BFrames { get; set; }
    [JsonPropertyName("display_aspect_ratio")]
    public string DisplayAspectRatio { get; set; }
    [JsonPropertyName("r_frame_rate")]
    public string RFrameRate { get; set; }
    [JsonPropertyName("avg_frame_rate")]
    public string AverageFrameRate { get; set; }

    // CodecType Audio
    [JsonPropertyName("channels")]
    public short Channels { get; set; }
    [JsonPropertyName("channel_layout")]
    public string ChannelLayout { get; set; }
}

public class Tags
{
    [JsonPropertyName("language")]
    public string Language { get; set; }
    [JsonPropertyName("title")]
    public string Title { get; set; }
    [JsonPropertyName("NUMBER_OF_FRAMES-eng")]
    public int NumberOfFrames { get; set; }
}

public class Disposition
{
    [JsonConverter(typeof(BoolJsonConverter))]
    [JsonPropertyName("default")]
    public bool Default { get; set; }
    [JsonConverter(typeof(BoolJsonConverter))]
    [JsonPropertyName("comment")]
    public bool Commentary { get; set; }
    [JsonConverter(typeof(BoolJsonConverter))]
    [JsonPropertyName("forced")]
    public bool Forced { get; set; }
    [JsonConverter(typeof(BoolJsonConverter))]
    [JsonPropertyName("hearing_impaired")]
    public bool HearingImpaired { get; set; }
    [JsonConverter(typeof(BoolJsonConverter))]
    [JsonPropertyName("visual_impaired")]
    public bool VisualImpaired { get; set; }
}

public class Format
{
    [JsonPropertyName("tags")]
    public Tags Tags { get; set; }

    [JsonPropertyName("filename")]
    public string FileName { get; set; }
    [JsonPropertyName("nb_streams")]
    public int NumberOfStreams { get; set; }
    [JsonPropertyName("duration")]
    public double DurationInSeconds { get; set; }
    [JsonPropertyName("size")]
    public long FileSizeInBytes { get; set; }
}
