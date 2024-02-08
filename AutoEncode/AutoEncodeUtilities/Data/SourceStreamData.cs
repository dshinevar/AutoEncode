using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace AutoEncodeUtilities.Data
{
    public class SourceStreamData(int durationInSeconds, int numberOfFrames, VideoStreamData videoStream, IEnumerable<AudioStreamData> audioStreams, IEnumerable<SubtitleStreamData> subtitleStreams) : ISourceStreamData
    {
        public int DurationInSeconds { get; } = durationInSeconds;
        /// <summary>This is an approx. number; Used for dolby vision jobs</summary>
        public int NumberOfFrames { get; } = numberOfFrames;
        public VideoStreamData VideoStream { get; set; } = videoStream;
        public IEnumerable<AudioStreamData> AudioStreams { get; set; } = audioStreams.ToList();
        public IEnumerable<SubtitleStreamData> SubtitleStreams { get; set; } = subtitleStreams?.ToList();
    }

    public abstract class StreamData
    {
        public short StreamIndex { get; set; } = -1;
        public string Title { get; set; }
    }

    public class VideoStreamData : 
        StreamData
    {
        public HDRData HDRData { get; set; }
        public bool HasHDR => (!HDRData?.HDRFlags.Equals(HDRFlags.NONE)) ?? false;
        public bool HasDynamicHDR => HasHDR && (HDRData?.IsDynamic ?? false);
        public string CodecName { get; set; }
        public string PixelFormat { get; set; }
        /// <summary> Crop string should be in this format as it allows it to be dropped into the ffmpeg command: XXXX:YYYY:AA:BB </summary>
        public string Crop { get; set; }
        public string Resolution { get; set; }
        public int ResoultionInt { get; set; }
        public string ColorSpace { get; set; }
        public string ColorPrimaries { get; set; }
        public string ColorTransfer { get; set; }
        public string FrameRate { get; set; }
        public bool Animated { get; set; } = false;
        public VideoScanType ScanType { get; set; } = VideoScanType.UNDETERMINED;
        public ChromaLocation? ChromaLocation { get; set; } = null;
    }

    public class HDRData : IUpdateable<HDRData>
    {
        public HDRFlags HDRFlags { get; set; } = HDRFlags.NONE;
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
        public string MaxCLL { get; set; }
        public Dictionary<HDRFlags, string> DynamicMetadataFullPaths { get; set; }
        public bool IsDynamic => HDRFlags.HasFlag(HDRFlags.HDR10PLUS) || HDRFlags.HasFlag(HDRFlags.DOLBY_VISION);
        public void Update(HDRData data) => data.CopyProperties(this);
    }

    public class AudioStreamData : 
        StreamData,
        IUpdateable<AudioStreamData>
    {
        public short AudioIndex { get; set; } = -1;
        public string CodecName { get; set; }
        public string Descriptor { get; set; }
        public short Channels { get; set; }
        public string ChannelLayout { get; set; }
        public string Language { get; set; }
        public bool Commentary { get; set; }
        public void Update(AudioStreamData data) => data.CopyProperties(this);
        public override bool Equals(object obj)
        {
            if (obj is AudioStreamData data)
            {
                return (StreamIndex == data.StreamIndex) &&
                        (AudioIndex == data.AudioIndex) &&
                        CodecName.Equals(data.CodecName);
            }

            return false;
        }

        public override int GetHashCode() => StreamIndex.GetHashCode() ^ AudioIndex.GetHashCode() ^ CodecName.GetHashCode();
    }

    public class SubtitleStreamData : 
        StreamData,
        IUpdateable<SubtitleStreamData>
    {
        public short SubtitleIndex { get; set; } = -1;
        public string Language { get; set; }
        public string Descriptor { get; set; }
        public bool Forced { get; set; }

        public void Update(SubtitleStreamData data) => data.CopyProperties(this);
        public override bool Equals(object obj)
        {
            if (obj is SubtitleStreamData data)
            {
                return (StreamIndex == data.StreamIndex) &&
                        (SubtitleIndex == data.SubtitleIndex);
            }

            return false;
        }

        public override int GetHashCode() => StreamIndex.GetHashCode() ^ SubtitleIndex.GetHashCode();
    }
}
