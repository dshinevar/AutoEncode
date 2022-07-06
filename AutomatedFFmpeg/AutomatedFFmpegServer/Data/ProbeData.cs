using System.Collections.Generic;

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
    }
}
