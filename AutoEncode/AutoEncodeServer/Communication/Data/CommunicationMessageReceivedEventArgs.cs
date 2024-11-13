using AutoEncodeUtilities.Communication.Data;
using NetMQ;
using System;

namespace AutoEncodeServer.Communication.Data;

public class CommunicationMessageReceivedEventArgs(NetMQFrame clientAddress, CommunicationMessage message) : EventArgs
{
    public NetMQFrame ClientAddress { get; } = clientAddress;

    public CommunicationMessage Message { get; } = message;
}
