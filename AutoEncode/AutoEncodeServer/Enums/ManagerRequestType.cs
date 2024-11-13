namespace AutoEncodeServer.Enums;

/// <summary>The type of request a manager class needs to process.</summary>
public enum ManagerRequestType
{
    // Common -- if any
    /// <summary>Default -- nothing to do. Shouldn't really be used.</summary>
    Nothing = 0,

    // Source File Manager Tasks
    UpdateSourceFileEncodingStatus = 100,

    #region Encoding Job Manager Tasks
    RemoveEncodingJobById = 200,

    CancelJobById,

    PauseJobById,

    ResumeJobById,

    PauseAndCancelJobById

    #endregion Encoding Job Manager Tasks
}
