using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Communication.Enums;

/// <summary>The type of request message from the client.</summary>
public enum RequestMessageType
{
    /// <summary>Default -- should be unused.</summary>
    None = 0,

    [Display(Name = "Cancel Request", ShortName = "Cancel Request", Description = "Cancel Encoding Job Request Message")]
    CancelRequest,

    [Display(Name = "Pause Request", ShortName = "Pause Request", Description = "Pause Encoding Job Request Message")]
    PauseRequest,

    [Display(Name = "Resume Request", ShortName = "Resume Request", Description = "Resume Encoding Job Request Message")]
    ResumeRequest,

    [Display(Name = "Pause and Cancel Request", ShortName = "Pause and Cancel Request", Description = "Pause and Cancel Encoding Job Request Message")]
    PauseCancelRequest,

    [Display(Name = "Encode Request", ShortName = "Encode Request", Description = "Encode Encoding Job Request Message")]
    EncodeRequest,

    [Display(Name = "Bulk Encode Request", ShortName = "Bulk Encode Request", Description = "Bulk Encode Encoding Job Request Message")]
    BulkEncodeRequest,

    [Display(Name = "Source Files Request", ShortName = "Source Files Request", Description = "Source Files Request Message")]
    SourceFilesRequest,

    [Display(Name = "Remove Job Request", ShortName = "Remove Job Request", Description = "Remove Encoding Job Request Message")]
    RemoveJobRequest,

    [Display(Name = "Job Queue Request", ShortName = "Job Queue Request", Description = "Job Queue Request Message")]
    JobQueueRequest,
}
