using System.ComponentModel.DataAnnotations;

namespace AutomatedFFmpegUtilities.Enums
{
    public enum HDRType
    {
        [Display(Name = "None", Description = "No HDR", ShortName = "None")]
        NONE = 0,
        [Display(Name = "HDR10", Description = "HDR10", ShortName = "HDR10")]
        HDR10 = 1,
        [Display(Name = "HDR10+", Description = "HDR10+", ShortName = "HDR10+")]
        HDR10PLUS = 2,
        [Display(Name = "Dolby Vision", Description = "Dolby Vision", ShortName = "DV")]
        DOLBY_VISION = 3
    }
}
