using System.ComponentModel;

namespace AutoEncodeUtilities.Enums
{
    public enum AEWorkerThreadStatus
    {
        [Description("Processing")]
        PROCESSING = 0,
        [Description("Sleeping")]
        SLEEPING = 1,
        [Description("Deep Sleeping")]
        DEEP_SLEEPING = 2
    }
}
