using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Enums;

public enum ChromaLocation
{
    [Display(Name = "Left", Description = "Left (Default)", ShortName = "left")]
    LEFT_DEFAULT = 0,
    [Display(Name = "Center", Description = "Center", ShortName = "center")]
    CENTER = 1,
    [Display(Name = "Top Left", Description = "Top Left", ShortName = "topleft")]
    TOP_LEFT = 2,
    [Display(Name = "Top", Description = "Top", ShortName = "top")]
    TOP = 3,
    [Display(Name = "Bottom Left", Description = "Bottom Left", ShortName = "bottomleft")]
    BOTTOM_LEFT = 4,
    [Display(Name = "Bottom", Description = "Bottom", ShortName = "bottom")]
    BOTTOM = 5
}
