using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using NetMQ;
using System;

namespace AutoEncodeServer.Communication.Data;

public class RequestMessageReceivedEventArgs(NetMQFrame clientAddress, CommunicationMessage<RequestMessageType> message) : EventArgs
{
    public NetMQFrame ClientAddress { get; } = clientAddress;

    public CommunicationMessage<RequestMessageType> Message { get; } = message;
}
