using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeClient.Comm
{
    public class ClientUpdateService : IDisposable
    {
        private readonly SubscriberSocket _subscriberSocket = null;
        private readonly NetMQPoller _poller = null;
        private readonly NetMQMonitor _monitor = null;
        private readonly ILogger _logger = null;
        private readonly ManualResetEvent _flowControlMRE = new(true);


        public event EventHandler<List<EncodingJobData>> DataReceived;
        public bool Connected { get; private set; }
        public string IpAddress { get; }
        public int Port { get; }
        public string ConnectionString => $"tcp://{IpAddress}:{Port}";

        public ClientUpdateService(ILogger logger, string ipAddress, int port)
        {
            _logger = logger;
            IpAddress = ipAddress;
            Port = port;

            _subscriberSocket = new SubscriberSocket();
            _subscriberSocket.Options.ReceiveHighWatermark = 1;
            _poller = new NetMQPoller { _subscriberSocket };
            _monitor = new(_subscriberSocket, $"inproc://req.inproc", SocketEvents.Connected | SocketEvents.Disconnected);
            _monitor.Connected += Monitor_Connected;
            _monitor.Disconnected += Monitor_Disconnected;

            _subscriberSocket.ReceiveReady += async (s, a) =>
            {
                string topic = string.Empty;
                string message = string.Empty;

                if (_flowControlMRE.WaitOne(0) is false) return;
                _flowControlMRE.Reset();

                try
                {
                    topic = _subscriberSocket?.ReceiveFrameString();
                    message = _subscriberSocket?.ReceiveFrameString();

                    if (string.IsNullOrWhiteSpace(message) is false && message.IsValidJson() is true)
                    {
                        List<EncodingJobData> clientUpdateData = JsonConvert.DeserializeObject<List<EncodingJobData>>(message, CommunicationConstants.SerializerSettings);

                        if (clientUpdateData is not null)
                        {
                            await Task.Factory.StartNew(() => DataReceived?.Invoke(this, clientUpdateData));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, "Error with Client Update", "ClientUpdateService", new { topic, message, ConnectionString });
                }

                _flowControlMRE.Set();
            };
        }

        #region Public Methods
        public void Start()
        {
            try
            {
                _subscriberSocket.Connect(ConnectionString);
                _subscriberSocket.Subscribe(CommunicationConstants.ClientUpdateTopic);
                _poller.RunAsync();
                _monitor.StartAsync();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to Start ClientUpdateService", nameof(ClientUpdateService), new { ConnectionString });
            }
        }

        public void Stop()
        {
            try
            {
                _monitor.Stop();
                _monitor.Dispose();
                _poller.Stop();
                _poller.Dispose();
                _subscriberSocket.Unsubscribe(CommunicationConstants.ClientUpdateTopic);
                _subscriberSocket.Close();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Failed to Stop ClientUpdateService", nameof(ClientUpdateService));
            }
        }

        public void Dispose() => Stop();
        #endregion Public Methods

        private void Monitor_Disconnected(object sender, NetMQMonitorSocketEventArgs e)
        {
            Connected = false;
            _logger.LogInfo($"Disconnected from {e.Address}", nameof(CommunicationManager));
        }

        private void Monitor_Connected(object sender, NetMQMonitorSocketEventArgs e)
        {
            Connected = true;
            _logger.LogInfo($"Connected to {e.Address}", nameof(ClientUpdateService));
        }
    }
}
