using AutoEncodeUtilities.Enums;

namespace AutoEncodeUtilities.Messages
{
    public class SourceFileRefreshMessage : AEMessageBase
    {
        public SourceFileRefreshMessage() => MessageType = AEMessageType.CLIENT_REQUEST;
    }
}
