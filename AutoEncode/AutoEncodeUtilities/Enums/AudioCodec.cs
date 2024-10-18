using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Enums;

public enum AudioCodec
{
    [Display(Name = "Unknown", Description = "Unknown", ShortName = "Unknown")]
    UNKNOWN = 0,
    /// <summary> Copy of the source audio </summary>
    [Display(Name = "Copy", Description = "copy", ShortName = "copy")]
    COPY = 1,
    [Display(Name = "AAC", Description = "aac", ShortName = "aac")]
    AAC = 2,
    [Display(Name = "Opus", Description = "opus", ShortName = "opus")]
    OPUS = 3,
}
