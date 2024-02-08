using AutoEncodeServer.Communication;
using NetMQ;
using System;

namespace AutoEncodeServer.Interfaces
{
    public interface ICommunicationManager
    {
        string ConnectionString { get; }

        int Port { get; }

        event EventHandler<AEMessageReceivedArgs> MessageReceived;

        bool Start(int port);

        bool Stop();

        void SendMessage<T>(NetMQFrame clientAddress, T obj);
    }
}
