using AutoEncodeServer.Communication.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Communication;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Logger;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.Communication;

public class ClientUpdatePublisher : IClientUpdatePublisher
{
    private struct ClientUpdateRequest
    {
        public string Topic { get; set; }
        public CommunicationMessage<ClientUpdateType> Message { get; set; }
    }

    #region Dependencies
    public ILogger Logger { get; set; }
    #endregion Dependencies

    private bool _initialized = false;
    private readonly PublisherSocket _publisherSocket = new();
    private readonly BlockingCollection<ClientUpdateRequest> _requests = [];
    private Task _requestHandlerTask = null;
    private ManualResetEvent _shutdownMRE;
    private readonly CancellationTokenSource _shutdownCancellationTokenSource = new();

    #region Properties
    public string ConnectionString => $"tcp://*:{Port}";
    public int Port { get; set; }
    #endregion Properties

    /// <summary>Default Constructor</summary>
    public ClientUpdatePublisher() { }

    #region Initialize / Start / Shutdown
    public void Initialize(ManualResetEvent shutdownMRE)
    {
        if (_initialized is false)
        {
            _shutdownMRE = shutdownMRE;
            _shutdownMRE.Reset();
            Port = State.ConnectionSettings.ClientUpdatePort;
        }

        _initialized = true;
        HelperMethods.DebugLog($"{nameof(ClientUpdatePublisher)} Initialized", nameof(ClientUpdatePublisher));
    }

    public void Start()
    {
        try
        {
            if (_initialized is false)
                throw new InvalidOperationException($"{nameof(ClientUpdatePublisher)} is not initialized.");

            _publisherSocket.Bind(ConnectionString);

            _requestHandlerTask = StartRequestHandler();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to start {nameof(ClientUpdatePublisher)}", nameof(ClientUpdatePublisher), new { ConnectionString });
            throw;
        }

        Logger.LogInfo($"{nameof(ClientUpdatePublisher)} binded to {ConnectionString} -- Request Thread started.", nameof(ClientUpdatePublisher));
    }

    public void Stop()
    {
        try
        {
            // Stop adding and clear requests
            _requests.CompleteAdding();
            while (_requests.TryTake(out _));

            _shutdownCancellationTokenSource.Cancel();

            _publisherSocket?.Unbind(ConnectionString);
            _publisherSocket?.Close();

            try
            {
                _requestHandlerTask?.Wait();
            }
            catch (OperationCanceledException) { }            

            Logger.LogInfo($"{nameof(ClientUpdatePublisher)} unbound from {ConnectionString} -- Request Thread stopped.", nameof(ClientUpdatePublisher));
            _shutdownMRE.Set();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to stop {nameof(ClientUpdatePublisher)}", nameof(ClientUpdatePublisher));
            throw;
        }
    }
    #endregion Initialize / Start / Shutdown

    #region Request Handling
    private Task StartRequestHandler()
        => Task.Run(() =>
        {
            try
            {
                foreach (ClientUpdateRequest request in _requests.GetConsumingEnumerable(_shutdownCancellationTokenSource.Token))
                {
                    SendUpdateToClients(request);
                }
            }
            catch (OperationCanceledException) { }

        }, _shutdownCancellationTokenSource.Token);

    private void SendUpdateToClients(ClientUpdateRequest request)
    {
        // Just don't do anything if we are shutting down / have shut down
        if ((_shutdownCancellationTokenSource.IsCancellationRequested is true) ||
            (_publisherSocket is null) ||
            (_publisherSocket.IsDisposed is true))
        {
            return;
        }

        try
        {
            string serializedData = JsonSerializer.Serialize(request.Message, CommunicationConstants.SerializerOptions);

            if (string.IsNullOrWhiteSpace(serializedData) is false)
            {
                _publisherSocket.SendMoreFrame(request.Topic).SendFrame(serializedData);
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Error sending a client update", nameof(ClientUpdatePublisher), new { ConnectionString, request.Topic });
        }
    }

    public bool AddClientUpdateRequest(string topic, CommunicationMessage<ClientUpdateType> message)
        => _requests.TryAdd(new ClientUpdateRequest()
        {
            Topic = topic,
            Message = message
        });
    #endregion Request Handling
}
