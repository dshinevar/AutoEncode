using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Enums
{
    /// <summary>The Status of the Worker Thread</summary>
    public enum AEWorkerThreadStatus
    {
        /// <summary>Worker Thread Starting</summary>
        [Display(Name = "Starting", Description = "Starting Up Worker Thread", ShortName = "Starting")]
        Starting = 0,

        /// <summary>Worker Thread Stopping</summary>
        [Display(Name = "Stopping", Description = "Stopping Worker Thread", ShortName = "Stopping")]
        Stopping = 1,

        /// <summary>Worker Thread Processing</summary>
        [Display(Name = "Processing", Description = "Worker Thread Is Processing", ShortName = "Processing")]
        Processing = 2,

        /// <summary>Worker Thread Sleeping</summary>
        [Display(Name = "Sleeping", Description = "Worker Thread Is Sleeping", ShortName = "Sleeping")]
        Sleeping = 3
    }
}
