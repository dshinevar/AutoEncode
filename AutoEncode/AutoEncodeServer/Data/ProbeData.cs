﻿using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoEncodeServer.Data
{
#pragma warning disable CS0649
    /// <summary>
    /// Class to contain data pulled from ffprobe;  Lower-case is intentional for json serialization
    /// </summary>
    internal class ProbeData
    {
        public List<Frame> frames;
        public List<Stream> streams;
        public Format format;

        internal class Frame
        {
            public string media_type;
            public int stream_index;
            public string pix_fmt;
            public string color_space;
            public string color_primaries;
            public string color_transfer;
            public string chroma_location;
            public List<SideData> side_data_list;
        }

        internal class SideData
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

        internal class Stream
        {
            public int index;
            public string codec_type;
            public string codec_name;
            public string profile;
            public int width;
            public int height;
            public string pix_fmt;
            public string color_space;
            public string color_transfer;
            public string color_primaries;
            public string chroma_location;
            public int channels;
            public string channel_layout;
            public string r_frame_rate;
            public string avg_frame_rate;
            public Tags tags;
            public Disposition disposition;
        }

        internal class Tags
        {
            public string language;
            public string title;
            [JsonProperty(PropertyName = "NUMBER_OF_FRAMES-eng")]
            public int number_of_frames;
        }

        internal class Disposition
        {
            public int @default;
            public int comment;
            public int forced;
        }

        internal class Format
        {
            public int nb_streams;
            public double duration; // seconds
        }

        private static ChromaLocation? ConvertStringToChromaLocation(string chromaLocationString)
        {
            switch (chromaLocationString)
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

        /// <summary> Converts to a <see cref="SourceStreamData"/> object</summary>
        /// <returns><see cref="SourceStreamData"/></returns>
        /// <exception cref="Exception">Throws if hdr data is supposedly found but is null or empty</exception>
        internal SourceStreamData ToSourceStreamData()
        {
            int numberOfFrames = 0;
            VideoStreamData videoStreamData = null;
            List<AudioStreamData> audioStreams = null;
            List<SubtitleStreamData> subtitleStreams = null;

            int audioIndex = 0;
            int subIndex = 0;
            foreach (Stream stream in streams)
            {
                if (stream.codec_type.Equals("video", StringComparison.OrdinalIgnoreCase) && videoStreamData is null)
                {
                    videoStreamData = new VideoStreamData()
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
                        Descriptor = stream.codec_name,
                        Title = stream.tags.title
                    };

                    if (string.IsNullOrWhiteSpace(stream.channel_layout) is false)
                    {
                        audioStream.ChannelLayout = stream.channel_layout;
                    }
                    else if (string.IsNullOrWhiteSpace(stream.tags.title) is false)
                    {
                        audioStream.ChannelLayout = stream.tags.title;
                    }
                    else
                    {
                        audioStream.ChannelLayout = $"{stream.channels}-channel(s)";
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

                    audioStreams ??= new();
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

                    subtitleStreams ??= new();
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

            if ((audioStreams?.Any() ?? false) is false)
            {
                throw new Exception("No audio streams found.");
            }

            // Handle additional Frame data (usually HDR data)
            foreach (Frame frame in frames)
            {
                // Currently don't do anything with audio (doesn't give much useful data)
                if (frame.media_type.Equals("video"))
                {
                    // Fallback data checks -- if somehow we still don't have data for these, grab from frame data -- will throw exceptions later if broken
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

                    if (videoStreamData.ChromaLocation is null)
                    {
                        videoStreamData.ChromaLocation = ConvertStringToChromaLocation(frame.chroma_location);
                    }

                    // Usually should have something in here
                    if (frame?.side_data_list?.Any() ?? false)
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

                            if (hdrData.IsDynamic) hdrData.DynamicMetadataFullPaths = new();

                            videoStreamData.HDRData = hdrData;
                        }
                    }
                }
            }

            // Final Validation
            if (string.IsNullOrWhiteSpace(videoStreamData.PixelFormat)) throw new Exception("No Pixel Format Found");

            if (string.IsNullOrWhiteSpace(videoStreamData.ColorPrimaries)) throw new Exception("No Color Primary Found");

            if (string.IsNullOrWhiteSpace(videoStreamData.ColorSpace)) throw new Exception("No Color Space Found");

            if (string.IsNullOrWhiteSpace(videoStreamData.ColorTransfer)) throw new Exception("No Color Transfer Found");

            if (videoStreamData.ChromaLocation is null) throw new Exception("No Chroma Location Found");

            return new SourceStreamData()
            {
                DurationInSeconds = Convert.ToInt32(format.duration),
                NumberOfFrames = numberOfFrames,
                VideoStream = videoStreamData,
                AudioStreams = audioStreams,
                SubtitleStreams = subtitleStreams
            };
        }
    }
#pragma warning restore CS0649
}
