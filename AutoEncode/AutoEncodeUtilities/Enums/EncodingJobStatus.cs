using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Enums;

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

public enum EncodingJobBuildingStatus
{
    [Display(Name = "Building (Default)", Description = "Building (Default)", ShortName = "Building")]
    BUILDING = 0,
    [Display(Name = "Probing", Description = "Probing File", ShortName = "Probing")]
    PROBING = 1,
    [Display(Name = "Scan Type", Description = "Determining Scan Type", ShortName = "Scan Type")]
    SCAN_TYPE = 2,
    [Display(Name = "Crop", Description = "Determining Crop", ShortName = "Crop")]
    CROP = 3,
    [Display(Name = "Dynamic HDR", Description = "Creating Dynamic HDR Files", ShortName = "Dynamic HDR")]
    DYNAMIC_HDR = 4,
    [Display(Name = "Instructions", Description = "Determining Encoding Instructions", ShortName = "Instructions")]
    INSTRUCTIONS = 5,
    [Display(Name = "Command", Description = "Creating FFmpeg Command", ShortName = "Command")]
    COMMAND = 6,
    [Display(Name = "Built", Description = "Built", ShortName = "Built")]
    BUILT = 7
}
