using System.ComponentModel;

namespace AutomatedFFmpegUtilities.Enums
{
    public enum VideoScanType : int
    {
        [Description("Interlaced Top Field First")]
        INTERLACED_TFF = 0,
        [Description("Interlaced Bottom Field First")]
        INTERLACED_BFF = 1,
        [Description("Progressive")]
        PROGRESSIVE = 2,
        [Description("Undetermined")]
        UNDETERMINED = 3
    }
}
