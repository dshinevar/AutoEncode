using AutomatedFFmpegUtilities.Enums;
using System.Collections.Generic;

namespace AutomatedFFmpegUtilities.Data
{
    public class SourceStreamData
    {
        public int DurationInSeconds { get; set; }
        public VideoStreamData VideoStream { get; set; }
        public List<AudioStreamData> AudioStreams { get; set; } = new();
        public List<SubtitleStreamData> SubtitleStreams { get; set; } = new();
    }

    public class StreamData
    {
        public int StreamIndex { get; set; } = -1;
        public string Title { get; set; }
    }

    public class VideoStreamData : StreamData
    {
        public HDRData HDRData { get; set; }
        public string CodecName { get; set; }
        public string PixelFormat { get; set; }
        /// <summary> Crop string should be in this format as it allows it to be dropped into the ffmpeg command: crop=XXXX:YYYY:AA:BB </summary>
        public string Crop { get; set; }
        public string Resolution { get; set; }
        public int ResoultionInt { get; set; }
        public string ColorSpace { get; set; }
        public string ColorPrimaries { get; set; }
        public string ColorTransfer { get; set; }
        public string MaxCLL { get; set; }
        public bool Animated { get; set; } = false;
        public VideoScanType ScanType { get; set; } = VideoScanType.UNDETERMINED;
        public ChromaLocation? ChromaLocation { get; set; } = null;
    }

    public class HDRData
    {
        public string Red_X { get; set; }
        public string Red_Y { get; set; }
        public string Green_X { get; set; }
        public string Green_Y { get; set; }
        public string Blue_X { get; set; }
        public string Blue_Y { get; set; }
        public string WhitePoint_X { get; set; }
        public string WhitePoint_Y { get; set; }
        public string MinLuminance { get; set; }
        public string MaxLuminance { get; set; }
    }

    public class AudioStreamData : StreamData
    {
        public int AudioIndex { get; set; } = -1;
        public string CodecName { get; set; }
        public string Descriptor { get; set; }
        public int Channels { get; set; }
        public string ChannelLayout { get; set; }
        public string Language { get; set; }
        public bool Commentary { get; set; }
    }

    public class SubtitleStreamData : StreamData
    {
        public int SubtitleIndex { get; set; } = -1;
        public string Language { get; set; }
        public string Descriptor { get; set; }
        public bool Forced { get; set; }
    }
}
