using AutoEncodeUtilities.Enums;
using System.Collections.Generic;
using System.Linq;

namespace AutoEncodeUtilities.Data
{
    public class EncodingInstructions
    {
        /// <summary> Contains encoding instructions for the video stream </summary>
        public VideoStreamEncodingInstructions VideoStreamEncodingInstructions { get; set; }
        /// <summary> 
        /// Contains encoding instructions for each audio stream.
        /// <para>The number of audio stream encoding instructions should match the number of audio streams in the output file.</para>
        /// </summary>
        public List<AudioStreamEncodingInstructions> AudioStreamEncodingInstructions { get; set; }
        /// <summary> 
        /// Contains encoding instructions for each subtitle stream.
        /// <para>The number of subtitle stream encoding instructions should match the number of subtitle streams in the output file.</para>
        /// </summary>
        public List<SubtitleStreamEncodingInstructions> SubtitleStreamEncodingInstructions { get; set; }

        /// <summary>Used for DolbyVision only where the source file gets split </summary>
        public string EncodedVideoFullPath { get; set; }
        /// <summary>Used for DolbyVision only where the source file gets split </summary>
        public string EncodedAudioSubsFullPath { get; set; }
    }

    public class VideoStreamEncodingInstructions
    {
        public VideoEncoder VideoEncoder { get; set; } = VideoEncoder.UNKNOWN;
        public bool Deinterlace { get; set; }
        public HDRFlags HDRFlags { get; set; }
        public bool HasHDR => !HDRFlags.Equals(HDRFlags.NONE);
        public bool HasDynamicHDR => HasDolbyVision || (HDRFlags.HasFlag(HDRFlags.HDR10PLUS) && DynamicHDRMetadataFullPaths.ContainsKey(HDRFlags.HDR10PLUS));
        public bool HasDolbyVision => HDRFlags.HasFlag(HDRFlags.DOLBY_VISION) && DynamicHDRMetadataFullPaths.ContainsKey(HDRFlags.DOLBY_VISION);
        public Dictionary<HDRFlags, string> DynamicHDRMetadataFullPaths { get; set; }
        public int BFrames { get; set; }
        public int CRF { get; set; }
        public string PixelFormat { get; set; }
        public bool Crop { get; set; }
    }

    public class AudioStreamEncodingInstructions
    {
        public AudioCodec AudioCodec { get; set; } = AudioCodec.UNKNOWN;
        public int SourceIndex { get; set; }
        public string Title { get; set; }
        public string Language { get; set; } // Used for sorting
        public bool Commentary { get; set; } // Used for sorting
    }

    public class SubtitleStreamEncodingInstructions
    {
        public int SourceIndex { get; set; }
        public string Title { get; set; }
        public bool Forced { get; set; }
    }
}
