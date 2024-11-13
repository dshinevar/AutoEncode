namespace AutoEncodeUtilities.Communication.Enums;

/// <summary>The type of client update. Used in constructing pub/sub topics as well.</summary>
public enum ClientUpdateType
{
    None = 0,

    #region Source File Updates
    SourceFilesUpdate = 10,
    #endregion Source File Updates

    #region Encoding Job Updates
    EncodingJobQueue = 20,

    EncodingJobStatus = 21,

    EncodingJobProcessingData = 22,

    EncodingJobEncodingProgress = 23,
    #endregion EncodingJobUpdates
}
