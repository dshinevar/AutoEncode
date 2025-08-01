using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Enums;

public enum Severity
{
    [Description("Debug")]
    [Display(Name = "Debug", ShortName = "Debug", Description = "Debug")]
    DEBUG = 0,

    [Description("Info")]
    [Display(Name = "Info", ShortName = "Info", Description = "Information")]
    INFO = 1,

    [Description("Warning")]
    [Display(Name = "Warning", ShortName = "Warning", Description = "Warning")]
    WARNING,

    [Description("Error")]
    [Display(Name = "Error", ShortName = "Error", Description = "Error")]
    ERROR = 3,

    [Description("Fatal")]
    [Display(Name = "Fatal", ShortName = "Fatal", Description = "Fatal")]
    FATAL = 4
}
