namespace AutoEncodeUtilities.Enums
{
    public enum AEMessageType
    {
        Connected = 0,
        Disconnected = 1,

        #region Status
        Status_MovieSourceFiles_Request,
        Status_MovieSourceFiles_Response,
        Status_ShowSourceFiles_Request,
        Status_ShowSourceFiles_Response,
        #endregion Status

        #region Commands
        Cancel_Request,
        Cancel_Response,
        Pause_Request,
        Pause_Response,
        Resume_Request,
        Resume_Response,
        Cancel_Pause_Request,
        Cancel_Pause_Response,
        #endregion Commands
    }
}
