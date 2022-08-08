using System.ComponentModel.DataAnnotations;

namespace AutomatedFFmpegUtilities.Enums
{
    public enum EncodingJobStatus
    {
        [Display(Name = "New", Description = "New", ShortName = "New")]
        NEW = 0,
        [Display(Name = "Building", Description = "Building", ShortName = "Building")]
        BUILDING = 1,
        [Display(Name = "Built", Description = "Built", ShortName = "Built")]
        BUILT = 2,
        [Display(Name = "Encoding", Description = "Encoding", ShortName = "Encoding")]
        ENCODING = 3,
        [Display(Name = "Encoded", Description = "Encoded", ShortName = "Encoded")]
        ENCODED = 4,
        [Display(Name = "Post-Processing", Description = "Post-Processing", ShortName = "Post-Processing")]
        POST_PROCESSING = 5,
        [Display(Name = "Post-Processed", Description = "Post-Processed", ShortName = "Post-Processed")]
        POST_PROCESSED = 6
    }
}
