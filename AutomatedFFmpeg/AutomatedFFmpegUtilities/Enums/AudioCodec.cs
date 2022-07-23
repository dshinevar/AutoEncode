using System.ComponentModel;

namespace AutomatedFFmpegUtilities.Enums
{
    public enum AudioCodec
    {
        [Description("Unknown")]
        UNKNOWN = 0,
        /// <summary> Copy of the source audio </summary>
        [Description("Copy")]
        COPY = 1,
        [Description("AAC")]
        AAC = 2,
        [Description("Opus")]
        OPUS = 3,
    }
}
