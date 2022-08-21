﻿using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomatedFFmpegServer.Data
{
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
            public int stream_index;
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
            public int index;
            public string codec_type;
            public string codec_name;
            public string profile;
            public int width;
            public int height;
            public string color_space;
            public string color_transfer;
            public string color_primaries;
            public string chroma_location;
            public int channels;
            public string channel_layout;
            public Tags tags;
            public Disposition disposition;
        }

        public class Tags
        {
            public string language;
            public string title;
        }

        public class Disposition
        {
            public int @default;
            public int comment;
            public int forced;
        }

        public class Format
        {
            public int nb_streams;
            public string duration; // seconds
        }

        /// <summary> Converts to a <see cref="SourceStreamData"/> object</summary>
        /// <returns><see cref="SourceStreamData"/></returns>
        /// <exception cref="Exception">Throws if hdr data is supposedly found but is null or empty</exception>
        public SourceStreamData ToSourceStreamData()
        {
            SourceStreamData sourceFileData = new()
            {
                DurationInSeconds = Convert.ToInt32(Convert.ToDouble(format.duration))
            };

            int audioIndex = 0;
            int subIndex = 0;
            foreach (Stream stream in streams)
            {
                if (stream.codec_type.Equals("video"))
                {
                    sourceFileData.VideoStream = new VideoStreamData()
                    {
                        StreamIndex = stream.index,
                        Resolution = $"{stream.width}x{stream.height}",
                        ResoultionInt = stream.width * stream.height,
                        CodecName = stream.codec_name,
                        Title = string.IsNullOrWhiteSpace(stream.tags.title) ? "Video" : stream.tags.title
                    };
                }
                else if (stream.codec_type.Equals("audio"))
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

                    if (stream.codec_name.Equals("dts"))
                    {
                        audioStream.CodecName = stream.profile;
                    }
                    else
                    {
                        audioStream.CodecName = stream.codec_name;
                    }

                    if (stream.disposition.comment == 1 || stream.tags.title.Contains("Commentary"))
                    {
                        audioStream.Commentary = true;
                    }

                    sourceFileData.AudioStreams.Add(audioStream);
                    audioIndex++;
                }
                else if (stream.codec_type.Equals("subtitle"))
                {
                    SubtitleStreamData subtitleStream = new()
                    {
                        StreamIndex = stream.index,
                        SubtitleIndex = subIndex,
                        Descriptor = stream.codec_name,
                        Language = stream.tags.language,
                        Forced = stream.disposition.forced == 1,
                        Title = stream.tags.title
                    };

                    sourceFileData.SubtitleStreams.Add(subtitleStream);
                    subIndex++;
                }
            }

            // Throw error if no video or audio data is found (no subs is fine)
            if (sourceFileData.VideoStream is null)
            {
                throw new Exception("No video stream found.");
            }

            if (sourceFileData.AudioStreams.Any() is false)
            {
                throw new Exception("No audio streams found.");
            }


            foreach (Frame frame in frames)
            {
                // Currently don't do anything with audio (doesn't give much useful data)
                if (frame.media_type.Equals("video"))
                {
                    sourceFileData.VideoStream.PixelFormat = string.IsNullOrWhiteSpace(frame.pix_fmt) ? "yuv420p10le" : frame.pix_fmt;
                    sourceFileData.VideoStream.ColorPrimaries = string.IsNullOrWhiteSpace(frame.color_primaries) ? "bt709" : frame.color_primaries;
                    sourceFileData.VideoStream.ColorSpace = string.IsNullOrWhiteSpace(frame.color_space) ? "bt709" : frame.color_space;
                    sourceFileData.VideoStream.ColorTransfer = string.IsNullOrWhiteSpace(frame.color_transfer) ? "bt709" : frame.color_transfer;

                    ChromaLocation? chroma = null;
                    switch (frame.chroma_location)
                    {
                        case "topleft":
                        {
                            chroma = ChromaLocation.TOP_LEFT;
                            break;
                        }
                        case "center":
                        {
                            chroma = ChromaLocation.CENTER;
                            break;
                        }
                        case "top":
                        {
                            chroma = ChromaLocation.TOP;
                            break;
                        }
                        case "bottomleft":
                        {
                            chroma = ChromaLocation.BOTTOM_LEFT;
                            break;
                        }
                        case "bottom":
                        {
                            chroma = ChromaLocation.BOTTOM;
                            break;
                        }
                        case "left":
                        default:
                        {
                            chroma = ChromaLocation.LEFT_DEFAULT;
                            break;
                        }
                    }
                    sourceFileData.VideoStream.ChromaLocation = chroma;

                    // Usually should have something in here
                    if (frame.side_data_list.Any())
                    {
                        // Should only be one of both of these
                        SideData masteringDisplayMetadata = frame.side_data_list.SingleOrDefault(x => x.side_data_type.Equals("Mastering display metadata"));
                        SideData contentLightLevelMetadata = frame.side_data_list.SingleOrDefault(x => x.side_data_type.Equals("Content light level metadata"));

                        // Has HDR; Otherwise, we can't do HDR so don't do anything more
                        if (masteringDisplayMetadata is not null && contentLightLevelMetadata is not null)
                        {
                            IHDRData hdrData;
                            // Check if HDR10+ or DolbyVision
                            if (frame.side_data_list.Any(x => x.side_data_type.Contains("Dolby Vision")))
                            {
                                hdrData = new DynamicHDRData()
                                {
                                    HDRType = HDRType.DOLBY_VISION
                                };
                            }
                            else if (frame.side_data_list.Any(x => x.side_data_type.Contains("HDR Dynamic Metadata") || x.side_data_type.Contains("HDR10+")))
                            {
                                hdrData = new DynamicHDRData()
                                {
                                    HDRType = HDRType.HDR10PLUS
                                };
                            }
                            else
                            {
                                hdrData = new HDR10Data()
                                {
                                    HDRType = HDRType.HDR10
                                };
                            }

                            hdrData.Blue_X = string.IsNullOrWhiteSpace(masteringDisplayMetadata.blue_x) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.blue_x.Split("/")[0];
                            hdrData.Blue_Y = string.IsNullOrWhiteSpace(masteringDisplayMetadata.blue_y) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.blue_y.Split("/")[0];
                            hdrData.Green_X = string.IsNullOrWhiteSpace(masteringDisplayMetadata.green_x) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.green_x.Split("/")[0];
                            hdrData.Green_Y = string.IsNullOrWhiteSpace(masteringDisplayMetadata.green_y) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.green_y.Split("/")[0];
                            hdrData.Red_X = string.IsNullOrWhiteSpace(masteringDisplayMetadata.red_x) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.red_x.Split("/")[0];
                            hdrData.Red_Y = string.IsNullOrWhiteSpace(masteringDisplayMetadata.red_y) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.red_y.Split("/")[0];
                            hdrData.WhitePoint_X = string.IsNullOrWhiteSpace(masteringDisplayMetadata.white_point_x) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.white_point_x.Split("/")[0];
                            hdrData.WhitePoint_Y = string.IsNullOrWhiteSpace(masteringDisplayMetadata.white_point_y) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.white_point_y.Split("/")[0];
                            hdrData.MaxLuminance = string.IsNullOrWhiteSpace(masteringDisplayMetadata.max_luminance) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.max_luminance.Split("/")[0];
                            hdrData.MinLuminance = string.IsNullOrWhiteSpace(masteringDisplayMetadata.min_luminance) ? throw new Exception("Invalid HDR Data") : masteringDisplayMetadata.min_luminance.Split("/")[0];

                            hdrData.MaxCLL = $"{contentLightLevelMetadata.max_content},{contentLightLevelMetadata.max_average}";

                            sourceFileData.VideoStream.HDRData = hdrData;
                        }
                    }
                }
            }

            return sourceFileData;
        }
    }
}
