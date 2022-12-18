using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;

namespace AutoEncodeUtilities.Messages
{
    public class ClientUpdateMessage : AEMessageBase<ClientUpdateData>
    {
        public ClientUpdateMessage() => MessageType = AEMessageType.CLIENT_UPDATE;
    }
}
