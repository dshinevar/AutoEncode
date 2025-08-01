using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Enums;

public enum CodecType
{
    [Display(Name = "Unknown", Description = "Unknown", ShortName = "Unknown")]
    Unknown = 0,

    [Display(Name = "Video", Description = "Video", ShortName = "video")]
    Video = 1,

    [Display(Name = "Audio", Description = "Audio", ShortName = "audio")]
    Audio = 2,

    [Display(Name = "Subtitle", Description = "Subtitle", ShortName = "subtitle")]
    Subtitle = 3,
}
