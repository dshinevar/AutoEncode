using System.ComponentModel;

namespace AutomatedFFmpegUtilities.Enums
{
    public enum VideoEncoder
    {
        [Description("Unknown")]
        UNKNOWN = 0,
        [Description("libx264")]
        LIBX264 = 1,
        [Description("libx265")]
        LIBX265 = 2,
    }
}
