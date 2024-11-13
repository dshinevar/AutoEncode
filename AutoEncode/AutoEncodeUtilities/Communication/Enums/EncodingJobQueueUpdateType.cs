namespace AutoEncodeUtilities.Communication.Enums;

/// <summary>Indicates the type of update to the encoding job queue.</summary>
public enum EncodingJobQueueUpdateType
{
    /// <summary>Default </summary>
    None = 0,

    /// <summary>New encoding job added to queue.</summary>
    Add = 1,

    /// <summary>Encoding job removed from the queue.</summary>
    Remove = 2,

    /// <summary>Encoding job position moved.</summary>
    Move = 3,
}
