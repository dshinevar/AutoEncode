using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;

namespace AutomatedFFmpegUtilities.Messages
{
    public class ClientConnectMessage : AFMessageBase
    {
        public ClientConnectData Data { get; set; }

        public ClientConnectMessage() => MessageType = AFMessageType.CLIENT_CONNECT;
    }
}
