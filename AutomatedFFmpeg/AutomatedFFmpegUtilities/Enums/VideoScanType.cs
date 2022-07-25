using System.ComponentModel.DataAnnotations;

namespace AutomatedFFmpegUtilities.Enums
{
    public enum VideoScanType : int
    {
        [Display(Name = "Interlaced TFF", Description = "Interlaced Top Field First", ShortName = "TFF")]
        INTERLACED_TFF = 0,
        [Display(Name = "Interlaced BFF", Description = "Interlaced Bottom Field First", ShortName = "BFF")]
        INTERLACED_BFF = 1,
        [Display(Name = "Progressive", Description = "Progressive", ShortName = "Prog")]
        PROGRESSIVE = 2,
        [Display(Name = "Undetermined", Description = "Undetermined", ShortName = "Undetermined")]
        UNDETERMINED = 3
    }
}
