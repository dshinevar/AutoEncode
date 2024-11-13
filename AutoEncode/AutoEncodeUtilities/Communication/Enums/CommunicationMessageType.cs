using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Communication.Enums;

public enum CommunicationMessageType
{
    [Display(Name = "Connected", ShortName = "Connected", Description = "Connected")]
    Connected = 0,
    [Display(Name = "Disconnected", ShortName = "Disconnected", Description = "Disconnected")]
    Disconnected = 1,

    [Display(Name = "Error", ShortName = "Error", Description = "Message indicating error")]
    Error,

    #region Commands
    [Display(Name = "Cancel Request", ShortName = "Cancel Request", Description = "Cancel Encoding Job Request Message")]
    CancelRequest,
    [Display(Name = "Cancel Response", ShortName = "Cancel Response", Description = "Cancel Encoding Job Response Message")]
    CancelResponse,

    [Display(Name = "Pause Request", ShortName = "Pause Request", Description = "Pause Encoding Job Request Message")]
    PauseRequest,
    [Display(Name = "Pause Response", ShortName = "Pause Response", Description = "Pause Encoding Job Response Message")]
    PauseResponse,

    [Display(Name = "Resume Request", ShortName = "Resume Request", Description = "Resume Encoding Job Request Message")]
    ResumeRequest,
    [Display(Name = "Resume Response", ShortName = "Resume Response", Description = "Resume Encoding Job Response Message")]
    ResumeResponse,

    [Display(Name = "Pause and Cancel Request", ShortName = "Pause and Cancel Request", Description = "Pause and Cancel Encoding Job Request Message")]
    PauseCancelRequest,
    [Display(Name = "Pause and Cancel Response", ShortName = "Pause and Cancel Response", Description = "Pause and Cancel Encoding Job Response Message")]
    PauseCancelResponse,

    [Display(Name = "Encode Request", ShortName = "Encode Request", Description = "Encode Encoding Job Request Message")]
    EncodeRequest,
    [Display(Name = "Encode Response", ShortName = "Encode Response", Description = "Encode Encoding Job Response Message")]
    EncodeResponse,

    [Display(Name = "Bulk Encode Request", ShortName = "Bulk Encode Request", Description = "Bulk Encode Encoding Job Request Message")]
    BulkEncodeRequest,
    [Display(Name = "Bulk Encode Response", ShortName = "Bulk Encode Response", Description = "Bulk Encode Encoding Job Response Message")]
    BulkEncodeResponse,

    [Display(Name = "Source Files Request", ShortName = "Source Files Request", Description = "Source Files Request Message")]
    SourceFilesRequest,
    [Display(Name = "Source Files Response", ShortName = "Source Files Response", Description = "Source Files Response Message")]
    SourceFilesResponse,

    [Display(Name = "Remove Job Request", ShortName = "Remove Job Request", Description = "Remove Encoding Job Request Message")]
    RemoveJobRequest,
    [Display(Name = "Remove Job Response", ShortName = "Remove Job Response", Description = "Remove Encoding Job Response Message")]
    RemoveJobResponse,

    [Display(Name = "Job Queue Request", ShortName = "Job Queue Request", Description = "Job Queue Request Message")]
    JobQueueRequest,
    [Display(Name = "Job Queue Response", ShortName = "Job Queue Response", Description = "Job Queue Response Message")]
    JobQueueResponse
    #endregion Commands
}
