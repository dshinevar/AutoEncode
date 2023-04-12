using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Enums
{
    public enum EncodingJobStatus
    {
        [Display(Name = "New", Description = "New", ShortName = "NEW")]
        NEW = 0,
        [Display(Name = "Building", Description = "Building", ShortName = "BUILDING")]
        BUILDING = 1,
        [Display(Name = "Built", Description = "Built", ShortName = "BUILT")]
        BUILT = 2,
        [Display(Name = "Encoding", Description = "Encoding", ShortName = "ENCODING")]
        ENCODING = 3,
        [Display(Name = "Encoded", Description = "Encoded", ShortName = "ENCODED")]
        ENCODED = 4,
        [Display(Name = "Post-Processing", Description = "Post-Processing", ShortName = "POST-PROCESSING")]
        POST_PROCESSING = 5,
        [Display(Name = "Post-Processed", Description = "Post-Processed", ShortName = "POST-PROCESSED")]
        POST_PROCESSED = 6
    }
}
