using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;

namespace AutomatedFFmpegUtilities.Messages
{
    public class ClientConnectMessage : AFMessageBase<ClientConnectData>
    {
        public ClientConnectMessage() => MessageType = AFMessageType.CLIENT_CONNECT;
    }
}
