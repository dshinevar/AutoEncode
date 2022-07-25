using System.ComponentModel.DataAnnotations;

namespace AutomatedFFmpegUtilities.Enums
{
    public enum VideoEncoder
    {
        [Display(Name = "Unknown", Description = "Unknown", ShortName = "Unknown")]
        UNKNOWN = 0,
        [Display(Name = "libx264", Description = "libx264", ShortName = "libx264")]
        LIBX264 = 1,
        [Display(Name = "libx265", Description = "libx265", ShortName = "libx265")]
        LIBX265 = 2,
    }
}
