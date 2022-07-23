using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutomatedFFmpegUtilities.Enums;

namespace AutomatedFFmpegUtilities.Data
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
    }

    public class VideoStreamEncodingInstructions
    {
        public VideoEncoder VideoEncoder { get; set; } = VideoEncoder.UNKNOWN;
        public bool Deinterlace { get; set; }
        public bool HasHDR { get; set; }
        public int BFrames { get; set; }
        public int CRF { get; set; }
    }

    public class AudioStreamEncodingInstructions
    {
        public AudioCodec AudioCodec { get; set; } = AudioCodec.UNKNOWN;
        public int SourceIndex { get; set; }
        public string Language { get; set; } // Used for sorting
        public bool Commentary { get; set;} // Used for sorting
    }

    public class SubtitleStreamEncodingInstructions
    {
        public int SourceIndex { get; set; }
        public bool Forced { get; set; }
    }
}
