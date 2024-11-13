namespace AutoEncodeServer.Enums;

/// <summary>Indicates the reason / source of why an encoding job is being removed. </summary>
public enum RemovedEncodingJobReason
{
    None = 0,

    Completed,

    Errored,

    UserRequested
}
