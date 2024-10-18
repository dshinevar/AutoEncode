using System.ComponentModel.DataAnnotations;

namespace AutoEncodeClient.Enums;

public enum AEDialogButtons
{
    [Display(Name = "OK", ShortName = "Ok", Description = "Ok")]
    Ok = 0,

    [Display(Name = "OK/Cancel", ShortName = "Ok/Cancel", Description = "Ok and Cancel")]
    Ok_Cancel = 1,
}

public enum AEDialogButtonResult
{
    [Display(Name = "OK", ShortName = "Ok", Description = "Ok")]
    Ok = 0,

    [Display(Name = "Cancel", ShortName = "Cancel", Description = "Cancel")]
    Cancel = 1,
}
