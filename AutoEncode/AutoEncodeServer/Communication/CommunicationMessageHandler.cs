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
using System.Threading;

namespace AutoEncodeServer.Communication;

public class CommunicationMessageHandler : ICommunicationMessageHandler
{
    #region Dependencies
    public ILogger Logger { get; set; }
    #endregion Dependencies

    #region Private Properties
    private bool _initialized = false;
    private readonly RouterSocket _routerSocket = new();
    private readonly NetMQPoller _poller = null;
    private ManualResetEvent _shutdownMRE;
    #endregion Private Properties

    #region Public Properties
    public string ConnectionString => $"tcp://*:{Port}";
    public int Port { get; set; }

    public event EventHandler<RequestMessageReceivedEventArgs> MessageReceived;
    #endregion Public Properties

    public CommunicationMessageHandler()
    {
        _poller = [_routerSocket];
    }

    #region Initialize / Start / Stop
    public void Initialize(ManualResetEvent shutdownMRE)
    {
        if (_initialized is false)
        {
            _shutdownMRE = shutdownMRE;
            _shutdownMRE.Reset();
            Port = State.ConnectionSettings.CommunicationPort;
            _routerSocket.ReceiveReady += RouterSocket_ReceiveReady;
        }

        _initialized = true;

        HelperMethods.DebugLog($"{nameof(CommunicationMessageHandler)} Initialized", nameof(CommunicationMessageHandler));
    }


    public void Start()
    {
        try
        {
            if (_initialized is false)
                throw new InvalidOperationException($"{nameof(CommunicationMessageHandler)} is not initialized.");

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
            _shutdownMRE.Set();
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

                    if (string.IsNullOrWhiteSpace(messageString) is false && messageString.IsValidJson())
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
