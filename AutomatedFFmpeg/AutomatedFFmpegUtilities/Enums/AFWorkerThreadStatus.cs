using System.ComponentModel;

namespace AutomatedFFmpegUtilities.Enums
{
    public enum AFWorkerThreadStatus
    {
        [Description("Processing")]
        PROCESSING = 0,
        [Description("Sleeping")]
        SLEEPING = 1,
        [Description("Deep Sleeping")]
        DEEP_SLEEPING = 2
    }
}
