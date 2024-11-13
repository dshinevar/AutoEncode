using AutoEncodeServer.Communication.Data;
using AutoEncodeUtilities.Communication.Data;
using NetMQ;
using System;
using System.Threading;

namespace AutoEncodeServer.Communication.Interfaces;

public interface ICommunicationMessageHandler
{
    string ConnectionString { get; }

    int Port { get; }

    /// <summary>Fires when a message is received.</summary>
    event EventHandler<CommunicationMessageReceivedEventArgs> MessageReceived;

    /// <summary>Sets up <see cref="CommunicationMessageHandler"/> </summary>
    void Initialize(ManualResetEvent shutdownMRE);

    /// <summary>Opens connections and starts listening.</summary>
    void Start();

    /// <summary>Closes connections.</summary>
    void Stop();

    /// <summary>Sends <see cref="CommunicationMessage"/> to the given client. </summary>
    /// <param name="clientAddress"></param>
    /// <param name="communicationMessage"></param>
    void SendMessage(NetMQFrame clientAddress, CommunicationMessage communicationMessage);
}
