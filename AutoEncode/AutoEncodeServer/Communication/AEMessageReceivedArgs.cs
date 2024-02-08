using AutoEncodeUtilities.Messages;
using NetMQ;

namespace AutoEncodeServer.Communication
{
    public class AEMessageReceivedArgs(NetMQFrame clientAddress, AEMessage message)
    {
        public NetMQFrame ClientAddress { get; } = clientAddress;

        public AEMessage Message { get; } = message;
    }
}
