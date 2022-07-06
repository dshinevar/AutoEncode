using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedFFmpegUtilities.Data
{
    public class SourceFileData
    {
        public VideoData VideoStream { get; set; }
    }

    public class VideoData
    {
        // TODO: ChromaLocation, Scan
        public HDRData HDRData { get; set; }
        public string Crop { get; set; }
        public string Resolution { get; set; }
        public int ResoultionInt { get; set; }
        public string ColorSpace { get; set; }
        public string ColorPrimaries { get; set; }
        public string ColorTransfer { get; set; }
        public string MaxCLL { get; set; }
        public bool Animated { get; set; } = false;
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
}
