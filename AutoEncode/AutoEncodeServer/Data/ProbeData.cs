using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AutoEncodeServer.Data;

#pragma warning disable CS0649
/// <summary>
/// Class to contain data pulled from ffprobe;  Lower-case is intentional for json serialization
/// </summary>
public class ProbeData
{
    public List<Frame> frames;
    public List<Stream> streams;
    public Format format;

    public class Frame
    {
        public string media_type;
        public short stream_index;
        public string pix_fmt;
        public string color_space;
        public string color_primaries;
        public string color_transfer;
        public string chroma_location;
        public List<SideData> side_data_list;
    }

    public class SideData
    {
        public string side_data_type;
        public string red_x;
        public string red_y;
        public string green_x;
        public string green_y;
        public string blue_x;
        public string blue_y;
        public string white_point_x;
        public string white_point_y;
        public string min_luminance;
        public string max_luminance;
        public int max_content;
        public int max_average;
    }

    public class Stream
    {
        public short index;
        public string codec_type;
        public string codec_name;
        public string codec_long_name;
        public string profile;
        public int width;
        public int height;
        public string pix_fmt;
        public string color_space;
        public string color_transfer;
        public string color_primaries;
        public string chroma_location;
        public short channels;
        public string channel_layout;
        public string r_frame_rate;
        public string avg_frame_rate;
        public Tags tags;
        public Disposition disposition;
    }

    public class Tags
    {
        public string language;
        public string title;
        [JsonPropertyName("NUMBER_OF_FRAMES-eng")]
        public int number_of_frames;
    }

    public class Disposition
    {
        public int @default;
        public int comment;
        public int forced;
        public int dub;
        public int original;
        public int hearing_impaired;
        public int visual_impaired;
    }

    public class Format
    {
        public int nb_streams;
        public double duration; // seconds
        public Tags tags;
    }

    private static ChromaLocation? ConvertStringToChromaLocation(string chromaLocationString)
    {
        switch (chromaLocationString.Trim())
        {
            case "topleft":
            {
                return ChromaLocation.TOP_LEFT;
            }
            case "center":
            {
                return ChromaLocation.CENTER;
            }
            case "top":
            {
                return ChromaLocation.TOP;
            }
            case "bottomleft":
            {
                return ChromaLocation.BOTTOM_LEFT;
            }
            case "bottom":
            {
                return ChromaLocation.BOTTOM;
            }
            case "left":
            {
                return ChromaLocation.LEFT_DEFAULT;
            }
            default:
            {
                return null;
            }
        }
    }

    private static string ConvertNumberOfChannelsToLayout(short channels)
    {
        return channels switch
        {
            0 => throw new Exception("Can't have 0 audio channels."),
            1 => "Mono",
            2 => "Stereo",
            _ => $"{channels}-channels"
        };
    }

    public string GetTitle()
        => format?.tags?.title;

    /// <summary> Converts to a <see cref="SourceStreamData"/> object</summary>
    /// <returns><see cref="SourceStreamData"/></returns>
    /// <exception cref="Exception">Throws if hdr data is supposedly found but is null or empty</exception>
    public SourceStreamData ToSourceStreamData()
    {
        int numberOfFrames = 0;
        VideoStreamData videoStreamData = null;
        List<AudioStreamData> audioStreams = null;
        List<SubtitleStreamData> subtitleStreams = null;

        short audioIndex = 0;
        short subIndex = 0;
        foreach (Stream stream in streams)
        {
            if (stream.codec_type.Equals("video", StringComparison.OrdinalIgnoreCase) && videoStreamData is null)
            {
                videoStreamData = new()
                {
                    StreamIndex = stream.index,
                    Resolution = $"{stream.width}x{stream.height}",
                    ResoultionInt = stream.width * stream.height,
                    CodecName = stream.codec_name,
                    Title = string.IsNullOrWhiteSpace(stream.tags.title) ? "Video" : stream.tags.title,
                    ChromaLocation = ConvertStringToChromaLocation(stream.chroma_location),
                    PixelFormat = stream.pix_fmt,
                    ColorPrimaries = stream.color_primaries,
                    ColorSpace = stream.color_space,
                    ColorTransfer = stream.color_transfer
                };

                string frameRateString = string.IsNullOrWhiteSpace(stream.r_frame_rate) ? (stream.avg_frame_rate ?? string.Empty) : stream.r_frame_rate;
                videoStreamData.FrameRate = frameRateString;

                if (string.IsNullOrWhiteSpace(frameRateString) is false)
                {
                    string[] frameRateStrings = stream.r_frame_rate.Split("/");
                    if (double.TryParse(frameRateStrings[0], out double frameRateNumerator) && double.TryParse(frameRateStrings[1], out double frameRateDenominator))
                    {
                        double frameRate = frameRateNumerator / frameRateDenominator;
                        int frames = (int)(frameRate * format.duration);

                        videoStreamData.CalculatedFrameRate = Math.Round(frameRate, 3);
                        numberOfFrames = frames;
                    }
                }
            }
            else if (stream.codec_type.Equals("audio", StringComparison.OrdinalIgnoreCase))
            {
                AudioStreamData audioStream = new()
                {
                    StreamIndex = stream.index,
                    AudioIndex = audioIndex,
                    Channels = stream.channels,
                    Language = stream.tags.language,
                    Descriptor = string.IsNullOrWhiteSpace(stream.profile) ? stream.codec_long_name : stream.profile,
                    Title = stream.tags.title
                };

                if (string.IsNullOrWhiteSpace(stream.channel_layout) is false)
                {
                    audioStream.ChannelLayout = stream.channel_layout;
                }
                else
                {
                    audioStream.ChannelLayout = ConvertNumberOfChannelsToLayout(stream.channels);
                }

                if (string.IsNullOrWhiteSpace(stream.codec_name) is false)
                {
                    if (stream.codec_name.Equals("dts", StringComparison.OrdinalIgnoreCase))
                    {
                        // Make sure profile is set, is not always -- fallback to codec_name
                        audioStream.CodecName = string.IsNullOrWhiteSpace(stream.profile) ? stream.codec_name : stream.profile;
                    }
                    else
                    {
                        audioStream.CodecName = stream.codec_name;
                    }
                }
                else
                {
                    throw new Exception($"Unable to determine audio codec name for stream index {stream.index}");
                }

                if (stream.disposition.comment == 1 || stream.tags.title.Contains("Commentary"))
                {
                    audioStream.Commentary = true;
                }

                audioStreams ??= [];
                audioStreams.Add(audioStream);
                audioIndex++;
            }
            else if (stream.codec_type.Equals("subtitle", StringComparison.OrdinalIgnoreCase))
            {
                SubtitleStreamData subtitleStream = new()
                {
                    StreamIndex = stream.index,
                    SubtitleIndex = subIndex,
                    Descriptor = stream.codec_name,
                    Language = stream.tags.language,
                    Forced = stream.disposition.forced == 1,
                    Title = stream.tags.title ?? string.Empty
                };

                subtitleStreams ??= [];
                subtitleStreams.Add(subtitleStream);
                subIndex++;
            }
        }

        // Early Validation
        // Throw error if no video or audio data is found (no subs is fine)
        if (videoStreamData is null)
        {
            throw new Exception("No video stream found.");
        }

        if ((audioStreams?.Count ?? -1) < 1)
        {
            throw new Exception("No audio streams found.");
        }

        // Handle additional Frame data (usually HDR data)
        foreach (Frame frame in frames)
        {
            // Currently don't do anything with audio (doesn't give much useful data)
            if (frame.media_type.Equals("video"))
            {
                // Fallback data checks -- if somehow we still don't have data for these, grab from frame data
                if (string.IsNullOrWhiteSpace(videoStreamData.PixelFormat))
                {
                    videoStreamData.PixelFormat = frame.pix_fmt;
                }

                if (string.IsNullOrWhiteSpace(videoStreamData.ColorPrimaries))
                {
                    videoStreamData.ColorPrimaries = frame.color_primaries;
                }

                if (string.IsNullOrWhiteSpace(videoStreamData.ColorSpace))
                {
                    videoStreamData.ColorSpace = frame.color_space;
                }

                if (string.IsNullOrWhiteSpace(videoStreamData.ColorTransfer))
                {
                    videoStreamData.ColorTransfer = frame.color_transfer;
                }

                videoStreamData.ChromaLocation ??= ConvertStringToChromaLocation(frame.chroma_location);

                // Usually should have something in here
                if ((frame?.side_data_list?.Count ?? -1) > 0)
                {
                    // Should only be one
                    SideData masteringDisplayMetadata = frame.side_data_list.SingleOrDefault(x => x.side_data_type.Equals("Mastering display metadata", StringComparison.OrdinalIgnoreCase));

                    // Has HDR; Otherwise, we can't do HDR so don't do anything more
                    if (masteringDisplayMetadata is not null)
                    {
                        SideData contentLightLevelMetadata = frame.side_data_list.SingleOrDefault(x => x.side_data_type.Equals("Content light level metadata", StringComparison.OrdinalIgnoreCase));

                        HDRData hdrData = new()
                        {
                            HDRFlags = HDRFlags.HDR10,
                            Blue_X = string.IsNullOrWhiteSpace(masteringDisplayMetadata.blue_x) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.blue_x.Split("/")[0],
                            Blue_Y = string.IsNullOrWhiteSpace(masteringDisplayMetadata.blue_y) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.blue_y.Split("/")[0],
                            Green_X = string.IsNullOrWhiteSpace(masteringDisplayMetadata.green_x) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.green_x.Split("/")[0],
                            Green_Y = string.IsNullOrWhiteSpace(masteringDisplayMetadata.green_y) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.green_y.Split("/")[0],
                            Red_X = string.IsNullOrWhiteSpace(masteringDisplayMetadata.red_x) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.red_x.Split("/")[0],
                            Red_Y = string.IsNullOrWhiteSpace(masteringDisplayMetadata.red_y) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.red_y.Split("/")[0],
                            WhitePoint_X = string.IsNullOrWhiteSpace(masteringDisplayMetadata.white_point_x) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.white_point_x.Split("/")[0],
                            WhitePoint_Y = string.IsNullOrWhiteSpace(masteringDisplayMetadata.white_point_y) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.white_point_y.Split("/")[0],
                            MaxLuminance = string.IsNullOrWhiteSpace(masteringDisplayMetadata.max_luminance) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.max_luminance.Split("/")[0],
                            MinLuminance = string.IsNullOrWhiteSpace(masteringDisplayMetadata.min_luminance) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.min_luminance.Split("/")[0],
                            MaxCLL = $"{contentLightLevelMetadata?.max_content ?? 0},{contentLightLevelMetadata?.max_average ?? 0}"
                        };
                        // Check if HDR10+ or DolbyVision
                        if (frame.side_data_list.Any(x => x.side_data_type.Contains("Dolby Vision")))
                        {
                            hdrData.HDRFlags |= HDRFlags.DOLBY_VISION;
                        }

                        if (frame.side_data_list.Any(x => x.side_data_type.Contains("HDR Dynamic Metadata") || x.side_data_type.Contains("HDR10+")))
                        {
                            hdrData.HDRFlags |= HDRFlags.HDR10PLUS;
                        }

                        if (hdrData.IsDynamic) hdrData.DynamicMetadataFullPaths = [];

                        videoStreamData.HDRData = hdrData;
                    }
                }
            }
        }

        return new SourceStreamData(Convert.ToInt32(format.duration), numberOfFrames, videoStreamData, audioStreams, subtitleStreams);
    }
}
#pragma warning restore CS0649

