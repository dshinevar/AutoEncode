using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;

namespace AutomatedFFmpegUtilities.Messages
{
    public class ClientUpdateMessage : AFMessageBase<ClientUpdateData>
    {
        public ClientUpdateMessage() => MessageType = AFMessageType.CLIENT_UPDATE;
    }
}
