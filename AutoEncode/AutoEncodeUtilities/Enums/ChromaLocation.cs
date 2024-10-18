using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Enums;

public enum ChromaLocation
{
    [Display(Name = "Left", Description = "Left (Default)", ShortName = "Left")]
    LEFT_DEFAULT = 0,
    [Display(Name = "Center", Description = "Center", ShortName = "Center")]
    CENTER = 1,
    [Display(Name = "Top Left", Description = "Top Left", ShortName = "Top Left")]
    TOP_LEFT = 2,
    [Display(Name = "Top", Description = "Top", ShortName = "Top")]
    TOP = 3,
    [Display(Name = "Bottom Left", Description = "Bottom Left", ShortName = "Bottom Left")]
    BOTTOM_LEFT = 4,
    [Display(Name = "Bottom", Description = "Bottom", ShortName = "Bottom")]
    BOTTOM = 5
}
