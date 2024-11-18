using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Communication.Enums;

/// <summary>The type of response message from the server.</summary>
public enum ResponseMessageType
{
    /// <summary>Default -- should be unused.</summary>
    None = 0,

    [Display(Name = "Cancel Response", ShortName = "Cancel Response", Description = "Cancel Encoding Job Response Message")]
    CancelResponse,

    [Display(Name = "Pause Response", ShortName = "Pause Response", Description = "Pause Encoding Job Response Message")]
    PauseResponse,

    [Display(Name = "Resume Response", ShortName = "Resume Response", Description = "Resume Encoding Job Response Message")]
    ResumeResponse,

    [Display(Name = "Pause and Cancel Response", ShortName = "Pause and Cancel Response", Description = "Pause and Cancel Encoding Job Response Message")]
    PauseCancelResponse,

    [Display(Name = "Encode Response", ShortName = "Encode Response", Description = "Encode Encoding Job Response Message")]
    EncodeResponse,

    [Display(Name = "Bulk Encode Response", ShortName = "Bulk Encode Response", Description = "Bulk Encode Encoding Job Response Message")]
    BulkEncodeResponse,

    [Display(Name = "Source Files Response", ShortName = "Source Files Response", Description = "Source Files Response Message")]
    SourceFilesResponse,

    [Display(Name = "Remove Job Response", ShortName = "Remove Job Response", Description = "Remove Encoding Job Response Message")]
    RemoveJobResponse,

    [Display(Name = "Job Queue Response", ShortName = "Job Queue Response", Description = "Job Queue Response Message")]
    JobQueueResponse,


    /// <summary>General Error Response</summary>
    Error = 999
}
