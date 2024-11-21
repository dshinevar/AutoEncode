using System.ComponentModel.DataAnnotations;

namespace AutoEncodeServer.Enums;

/// <summary>Indicates the reason / source of why an encoding job is being removed. </summary>
public enum RemovedEncodingJobReason
{
    [Display(Name = "None", Description = "None")]
    None = 0,

    [Display(Name = "Completed", Description = "Completed")]
    Completed,

    [Display(Name = "Errored", Description = "Errored")]
    Errored,

    [Display(Name = "User Requested", Description = "User Requested")]
    UserRequested
}
