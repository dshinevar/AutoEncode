using AutomatedFFmpegUtilities.Enums;

namespace AutomatedFFmpegUtilities.Messages
{
    public class SourceFileRefreshMessage : AFMessageBase
    {
        public SourceFileRefreshMessage() => MessageType = AFMessageType.CLIENT_REQUEST;
    }
}
