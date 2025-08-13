using AutoEncodeServer.Communication.Data;
using AutoEncodeServer.Communication.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Communication;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Logger;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Text.Json;

namespace AutoEncodeServer.Communication;

public class CommunicationMessageHandler : ICommunicationMessageHandler
{
    #region Dependencies
    public ILogger Logger { get; set; }
    #endregion Dependencies

    #region Private Properties
    private readonly RouterSocket _routerSocket = new();
    private readonly NetMQPoller _poller = null;
    #endregion Private Properties

    #region Public Properties
    public string ConnectionString => $"tcp://*:{Port}";
    public int Port { get; set; }

    public event EventHandler<RequestMessageReceivedEventArgs> MessageReceived;
    #endregion Public Properties

    public CommunicationMessageHandler()
    {
        _poller = [_routerSocket];
        _routerSocket.ReceiveReady += RouterSocket_ReceiveReady;

        Port = State.ConnectionSettings.CommunicationPort;
    }

    #region Start / Stop
    public void Start()
    {
        try
        {
            if (MessageReceived is null)
                throw new InvalidOperationException($"Attempted to start {nameof(CommunicationMessageHandler)} without subscribing.");

            _routerSocket.Bind(ConnectionString);
            _poller.RunAsync();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to start {nameof(CommunicationMessageHandler)}", nameof(CommunicationMessageHandler), new { ConnectionString });
            throw;
        }

        Logger.LogInfo($"{nameof(CommunicationMessageHandler)} binded to {ConnectionString} -- Listening for messages.", nameof(CommunicationMessageHandler));
    }

    public void Stop()
    {
        try
        {
            _poller.Stop();
            _poller.Dispose();
            _routerSocket.Close();

            Logger.LogInfo($"{nameof(CommunicationMessageHandler)} unbound from {ConnectionString} -- Stopped listening.", nameof(CommunicationMessageHandler));
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to stop {nameof(CommunicationMessageHandler)}", nameof(CommunicationMessageHandler), new { ConnectionString });
            throw;
        }
    }

    #endregion Initialize / Start / Stop

    private void RouterSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
    {
        try
        {
            NetMQMessage message = null;

            while (e.Socket.TryReceiveMultipartMessage(ref message))
            {
                if (message.FrameCount == 3)
                {
                    string messageString = message[2].ConvertToString();

                    if (messageString.IsValidJson())
                    {
                        CommunicationMessage<RequestMessageType> communicationMessage = JsonSerializer.Deserialize<CommunicationMessage<RequestMessageType>>(messageString, CommunicationConstants.SerializerOptions);

                        MessageReceived?.Invoke(this, new RequestMessageReceivedEventArgs(message[0], communicationMessage));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Error handling received message.", nameof(CommunicationMessageHandler), new { ConnectionString, EventArgs = e });
        }
    }

    public void SendMessage(NetMQFrame clientAddress, CommunicationMessage<ResponseMessageType> communicationMessage)
    {
        try
        {
            NetMQMessage message = new();
            message.Append(clientAddress);
            message.AppendEmptyFrame();

            var response = JsonSerializer.Serialize(communicationMessage, CommunicationConstants.SerializerOptions);

            message.Append(response);

            _routerSocket.SendMultipartMessage(message);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Error sending message.", nameof(CommunicationMessageHandler), new { ConnectionString, clientAddress, communicationMessage });
            throw;
        }
    }
}
