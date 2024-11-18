namespace AutoEncodeUtilities.Communication.Enums;

/// <summary>The type of client update. Used in constructing pub/sub topics as well.</summary>
public enum ClientUpdateType
{
    /// <summary>Default -- should be unused.</summary>
    None = 0,

    #region Source File Updates
    /// <summary>Indicates update to source files.</summary>
    SourceFilesUpdate = 10,
    #endregion Source File Updates

    #region Encoding Job Updates
    EncodingJobQueue = 20,

    EncodingJobStatus = 21,

    EncodingJobProcessingData = 22,

    EncodingJobEncodingProgress = 23,
    #endregion EncodingJobUpdates
}
