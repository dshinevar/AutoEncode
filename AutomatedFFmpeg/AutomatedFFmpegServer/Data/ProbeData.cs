using System;
using System.Linq;
using System.Collections.Generic;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;

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

        public SourceStreamData ToSourceFileData()
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
                        ResoultionInt = stream.width*stream.height,
                        CodecName = stream.codec_name
                    };
                }
                else if (stream.codec_type.Equals("audio"))
                {
                    AudioStreamData audioStream = new AudioStreamData()
                    {
                        StreamIndex = stream.index,
                        AudioIndex = audioIndex,
                        Channels = stream.channels,
                        Language = stream.tags.language,
                        Descriptor = stream.codec_name,
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
                    SubtitleStreamData subtitleStream = new SubtitleStreamData()
                    {
                        StreamIndex = stream.index,
                        SubtitleIndex = subIndex,
                        Descriptor = stream.codec_name,
                        Language = stream.tags.language,
                        Forced = stream.disposition.forced == 1
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

                    ChromaLocation chroma = ChromaLocation.LEFT_DEFAULT;
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

                    HDRData hdrData = null;
                    foreach (SideData sideData in frame.side_data_list)
                    {
                        if (sideData.side_data_type.Equals("Mastering display metadata"))
                        {
                            if (hdrData is null) hdrData = new HDRData();
                            hdrData.Blue_X = string.IsNullOrWhiteSpace(sideData.blue_x) ? string.Empty : sideData.blue_x.Split("/")[0];
                            hdrData.Blue_Y = string.IsNullOrWhiteSpace(sideData.blue_y) ? string.Empty : sideData.blue_y.Split("/")[0];
                            hdrData.Green_X = string.IsNullOrWhiteSpace(sideData.green_x) ? string.Empty : sideData.green_x.Split("/")[0];
                            hdrData.Green_Y = string.IsNullOrWhiteSpace(sideData.green_y) ? string.Empty : sideData.green_y.Split("/")[0];
                            hdrData.Red_X = string.IsNullOrWhiteSpace(sideData.red_x) ? string.Empty : sideData.red_x.Split("/")[0];
                            hdrData.Red_Y = string.IsNullOrWhiteSpace(sideData.red_y) ? string.Empty : sideData.red_y.Split("/")[0];
                            hdrData.WhitePoint_X = string.IsNullOrWhiteSpace(sideData.white_point_x) ? string.Empty : sideData.white_point_x.Split("/")[0];
                            hdrData.WhitePoint_Y = string.IsNullOrWhiteSpace(sideData.white_point_y) ? string.Empty : sideData.white_point_y.Split("/")[0];
                            hdrData.MaxLuminance = string.IsNullOrWhiteSpace(sideData.max_luminance) ? string.Empty : sideData.max_luminance.Split("/")[0];
                            hdrData.MinLuminance = string.IsNullOrWhiteSpace(sideData.min_luminance) ? string.Empty : sideData.min_luminance.Split("/")[0];
                        }
                        else if (sideData.side_data_type.Equals("Content light level metadata"))
                        {
                            sourceFileData.VideoStream.MaxCLL = $"{sideData.max_content},{sideData.max_average}";
                        }
                    }
                    sourceFileData.VideoStream.HDRData = hdrData;
                }
            }

            return sourceFileData;
        }
    }
}
