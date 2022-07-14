using System.ComponentModel;

namespace AutomatedFFmpegUtilities.Enums
{
    public enum EncodingJobStatus
    {
        [Description("New")]
        NEW = 0,
        [Description("Analyzing")]
        ANALYZING = 1,
        [Description("Analyzed")]
        ANALYZED = 2,
        [Description("Encoding")]
        ENCODING = 3,
        [Description("Complete")]
        COMPLETE = 4
    }
}
