using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;

namespace AutoEncodeUtilities.Messages
{
    public class ClientConnectMessage : AEMessageBase<ClientConnectData>
    {
        public ClientConnectMessage() => MessageType = AEMessageType.CLIENT_CONNECT;
    }
}
