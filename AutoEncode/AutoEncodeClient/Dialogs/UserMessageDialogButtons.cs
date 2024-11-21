using System.ComponentModel.DataAnnotations;

namespace AutoEncodeClient.Dialogs;

public enum UserMessageDialogButtons
{
    [Display(Name = "OK", ShortName = "Ok", Description = "Ok")]
    Ok = 0,

    [Display(Name = "OK/Cancel", ShortName = "Ok/Cancel", Description = "Ok and Cancel")]
    Ok_Cancel = 1,

    [Display(Name = "Yes/No", ShortName = "Yes/No", Description = "Yes and No")]
    Yes_No = 2,
}

public enum UserMessageDialogResult
{
    [Display(Name = "OK", ShortName = "Ok", Description = "Ok")]
    Ok = 0,

    [Display(Name = "Cancel", ShortName = "Cancel", Description = "Cancel")]
    Cancel = 1,

    [Display(Name = "Yes", ShortName = "Yes", Description = "Yes")]
    Yes = 2,

    [Display(Name = "No", ShortName = "No", Description = "No")]
    No = 3,
}
