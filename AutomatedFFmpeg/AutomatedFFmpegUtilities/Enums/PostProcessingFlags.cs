using System;
using System.ComponentModel.DataAnnotations;

namespace AutomatedFFmpegUtilities.Enums
{
    [Flags]
    public enum PostProcessingFlags
    {
        None = 0,
        [Display(Name = "Copy", Description = "Copy File To Other Destinations", ShortName = "Copy")]
        Copy = 1,
        [Display(Name = "Delete Source File", Description = "Delete Source File", ShortName = "Delete Source File")]
        DeleteSourceFile = 2
    }
}
