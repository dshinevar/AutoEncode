using System.ComponentModel.DataAnnotations;

namespace AutoEncodeUtilities.Enums;

public enum AEMessageType
{
    [Display(Name = "Connected", ShortName = "Connected", Description = "Connected")]
    Connected = 0,
    [Display(Name = "Disconnected", ShortName = "Disconnected", Description = "Disconnected")]
    Disconnected = 1,

    [Display(Name = "Error", ShortName = "Error", Description = "Message indicating error")]
    Error,

    #region Commands
    [Display(Name = "Cancel Request", ShortName = "Cancel Request", Description = "Cancel Encoding Job Request Message")]
    Cancel_Request,
    [Display(Name = "Cancel Response", ShortName = "Cancel Response", Description = "Cancel Encoding Job Response Message")]
    Cancel_Response,

    [Display(Name = "Pause Request", ShortName = "Pause Request", Description = "Pause Encoding Job Request Message")]
    Pause_Request,
    [Display(Name = "Pause Response", ShortName = "Pause Response", Description = "Pause Encoding Job Response Message")]
    Pause_Response,

    [Display(Name = "Resume Request", ShortName = "Resume Request", Description = "Resume Encoding Job Request Message")]
    Resume_Request,
    [Display(Name = "Resume Response", ShortName = "Resume Response", Description = "Resume Encoding Job Response Message")]
    Resume_Response,

    [Display(Name = "Cancel Pause Request", ShortName = "Cancel Pause Request", Description = "Cancel Then Pause Encoding Job Request Message")]
    Cancel_Pause_Request,
    [Display(Name = "Cancel Pause Response", ShortName = "Cancel Pause Response", Description = "Cancel Then Pause Encoding Job Response Message")]
    Cancel_Pause_Response,

    [Display(Name = "Encode Request", ShortName = "Encode Request", Description = "Encode Encoding Job Request Message")]
    Encode_Request,
    [Display(Name = "Encode Response", ShortName = "Encode Response", Description = "Encode Encoding Job Response Message")]
    Encode_Response,

    [Display(Name = "Source Files Request", ShortName = "Source Files Request", Description = "Source Files Request Message")]
    Source_Files_Request,
    [Display(Name = "Source Files Response", ShortName = "Source Files Response", Description = "Source Files Response Message")]
    Source_Files_Response,

    [Display(Name = "Remove Job Request", ShortName = "Remove Job Request", Description = "Remove Encoding Job Request Message")]
    Remove_Job_Request,
    [Display(Name = "Remove Job Response", ShortName = "Remove Job Response", Description = "Remove Encoding Job Response Message")]
    Remove_Job_Response,

    [Display(Name = "Job Queue Request", ShortName = "Job Queue Request", Description = "Job Queue Request Message")]
    Job_Queue_Request,
    [Display(Name = "Job Queue Response", ShortName = "Job Queue Response", Description = "Job Queue Response Message")]
    Job_Queue_Response
    #endregion Commands
}
