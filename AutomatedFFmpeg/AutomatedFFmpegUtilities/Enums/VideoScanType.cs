using System.ComponentModel;

namespace AutomatedFFmpegUtilities.Enums
{
    public enum VideoScanType : int
    {
        [Description("Undetermined")]
        UNDETERMINED = 0,
        [Description("Interlaced Top Field First")]
        INTERLACED_TFF = 1,
        [Description("Interlaced Bottom Field First")]
        INTERLACED_BFF = 2,
        [Description("Progressive")]
        PROGRESSIVE = 3
    }
}
