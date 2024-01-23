namespace AutoEncodeUtilities.Enums
{
    public enum AEMessageType
    {
        Connected = 0,
        Disconnected = 1,

        #region Commands
        Cancel_Request,
        Cancel_Response,
        Pause_Request,
        Pause_Response,
        Resume_Request,
        Resume_Response,
        Cancel_Pause_Request,
        Cancel_Pause_Response,
        Encode_Request,
        Encode_Response,
        Source_Files_Request,
        Source_Files_Response,
        #endregion Commands
    }
}
