namespace AutoEncodeUtilities.Enums
{
    public enum AEMessageType
    {
        Connected = 0,
        Disconnected = 1,

        #region Status
        Status_MovieSourceFiles_Response,
        Status_MovieSourceFiles_Request,
        Status_ShowSourceFiles_Response,
        Status_ShowSourceFiles_Request,
        Status_Queue_Response,
        Status_Queue_Request,
        #endregion Status
    }
}
