using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;

namespace AutomatedFFmpegUtilities.Messages
{
    public class ClientUpdateMessage : AFMessageBase
    {
        public ClientUpdateData Data { get; set; }

        public ClientUpdateMessage() => MessageType = AFMessageType.CLIENT_UPDATE;
    }
}
