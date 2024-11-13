using AutoEncodeClient.Communication.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Communication;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Logger;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AutoEncodeClient.Communication;

public class ClientUpdateSubscriber :
    IClientUpdateSubscriber,
    IDisposable
{
    #region Dependencies
    public ILogger Logger { get; set; }
    #endregion Dependencies

    private readonly SubscriberSocket _subscriberSocket = new();
    private readonly NetMQPoller _poller = null;
    private readonly NetMQMonitor _monitor = null;
    private readonly List<string> _subscribedTopics = [];

    #region Public Properties
    public bool Initialized { get; set; }

    public string ConnectionString => $"tcp://{IpAddress}:{Port}";

    public string IpAddress { get; set; }

    public int Port { get; set; }
    #endregion Public Properties

    public ClientUpdateSubscriber()
    {
        _poller = [_subscriberSocket];

        _subscriberSocket.ReceiveReady += SubscriberSocket_ReceiveReady;

        _monitor = new(_subscriberSocket, $"inproc://*:{Random.Shared.Next()}.monitor", SocketEvents.Connected | SocketEvents.Disconnected);
        _monitor.Connected += Socket_Connected;
        _monitor.Disconnected += Socket_Disconnected;
    }

    #region Init / Start / Stop
    public void Initialize()
    {
        if (Initialized is false)
        {
            IpAddress = State.ConnectionSettings.IPAddress;
            Port = State.ConnectionSettings.ClientUpdatePort;
        }

        Initialized = true;
    }

    public void Start()
    {
        if (Initialized is false)
            throw new InvalidOperationException($"{nameof(ClientUpdateSubscriber)} is not initialized.");

        try
        {
            _monitor.StartAsync();

            _subscriberSocket.Connect(ConnectionString);

            _poller.RunAsync();

            if (_subscribedTopics.Count == 0)
                Logger.LogWarning($"{nameof(ClientUpdateSubscriber)} is starting with no subscribed topics -- will receive all topics.");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Error starting {nameof(ClientUpdateSubscriber)}", nameof(ClientUpdateSubscriber), new { ConnectionString, _subscribedTopics });
            throw;
        }

        HelperMethods.DebugLog($"{nameof(ClientUpdateSubscriber)} started.", nameof(ClientUpdateSubscriber));
    }

    public void Stop()
    {
        try
        {
            _poller.Stop();

            if (_subscribedTopics.Count > 0)
            {
                Unsubscribe([.. _subscribedTopics]);
            }

            _subscriberSocket.Disconnect(ConnectionString);
            _subscriberSocket.Close();

            _monitor.Stop();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Error stopping {nameof(ClientUpdateSubscriber)}", nameof(ClientUpdateSubscriber), new { ConnectionString, _subscribedTopics });
        }
    }

    public void Dispose() => Stop();
    #endregion Init / Start / Stop

    #region Public Methods
    public void Subscribe(string topic)
        => Subscribe([topic]);

    public void Subscribe(IEnumerable<string> topics)
    {
        foreach (string topic in topics)
        {
            _subscriberSocket.Subscribe(topic);
            _subscribedTopics.Add(topic);
        }
    }

    public void Unsubscribe(string topic)
        => Unsubscribe([topic]);

    public void Unsubscribe(IEnumerable<string> topics)
    {
        foreach (string topic in topics)
        {
            _subscriberSocket.Unsubscribe(topic);
            _subscribedTopics.Remove(topic);
        }
    }
    #endregion Public Methods

    #region Events
    public event EventHandler<ClientUpdateMessage> ClientUpdateMessageReceived;

    private void SubscriberSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
    {
        try
        {
            while (e.Socket.TryReceiveFrameString(out string frameString, out bool more) is true)
            {
                string message = null;
                if (more is true)
                {
                    e.Socket.TryReceiveFrameString(out message);
                }

                if ((string.IsNullOrWhiteSpace(message) is false) && (message.IsValidJson() is true))
                {
                    ClientUpdateMessage updateMessage = JsonSerializer.Deserialize<ClientUpdateMessage>(message, CommunicationConstants.SerializerOptions);
                    ClientUpdateMessageReceived?.Invoke(this, updateMessage);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Error receiving messages.", nameof(ClientUpdateSubscriber), new { ConnectionString, _subscribedTopics, EventArgs = e });
        }
    }

    private void Socket_Connected(object sender, NetMQMonitorSocketEventArgs e)
        => HelperMethods.DebugLog($"Connected to {e.Address}", nameof(ClientUpdateSubscriber));

    private void Socket_Disconnected(object sender, NetMQMonitorSocketEventArgs e)
        => HelperMethods.DebugLog($"Disconnected from {e.Address}", nameof(ClientUpdateSubscriber));
    #endregion Events
}
