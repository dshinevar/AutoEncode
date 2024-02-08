using AutoEncodeClient.Data;
using AutoEncodeClient.Enums;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoEncodeClient.Communication
{
    public class ClientUpdateSubscriber : IClientUpdateSubscriber, IDisposable
    {
        #region Dependencies
        public ILogger Logger { get; set; }
        #endregion Dependencies

        #region Private Properties
        private bool _initialized = false;
        private readonly SubscriberSocket _subscriberSocket = null;
        private readonly NetMQPoller _poller = null;
        private Dictionary<string, ClientUpdateType> _subscribedTopicTypeLookup = null;
        #endregion Private Properties

        #region Public Properties
        public string ConnectionString => $"tcp://{IpAddress}:{Port}";

        public string IpAddress { get; set; }

        public int Port { get; set; }

        public event EventHandler<EncodingJobStatusUpdateData> StatusUpdateReceived;
        public event EventHandler<EncodingJobProcessingDataUpdateData> ProcessingDataUpdateReceived;
        public event EventHandler<EncodingJobEncodingProgressUpdateData> EncodingProgressUpdateReceived;
        public event EventHandler<IEnumerable<EncodingJobData>> QueueUpdateReceived;
        #endregion Public Properties

        public ClientUpdateSubscriber()
        {
            _subscriberSocket = new SubscriberSocket();
            _poller = [_subscriberSocket];

            _subscriberSocket.ReceiveReady += ClientUpdateReceived;
        }

        #region Public Methods
        public void Initialize(string ipAddress, int port, IEnumerable<SubscriberTopic> topics)
        {
            if (_initialized is false)
            {
                IpAddress = ipAddress;
                Port = port;
                _subscribedTopicTypeLookup = topics.ToDictionary(x => x.Topic, x => x.ClientUpdateType);

                _initialized = true;
            }
        }

        public void Start()
        {
            if (_initialized is false) throw new Exception($"{nameof(ClientUpdateSubscriber)} is not initialized.");

            try
            {
                _subscriberSocket.Connect(ConnectionString);
                foreach (string topic in _subscribedTopicTypeLookup.Keys)
                {
                    _subscriberSocket.Subscribe(topic);
                }

                _poller.RunAsync();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error starting {nameof(ClientUpdateSubscriber)}", nameof(ClientUpdateSubscriber));
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                _poller.Stop();

                foreach (string topic in _subscribedTopicTypeLookup.Keys)
                {
                    _subscriberSocket.Unsubscribe(topic);
                }

                _subscriberSocket.Disconnect(ConnectionString);

                _subscriberSocket.Close(); 
                
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error stopping {nameof(ClientUpdateSubscriber)}", nameof(ClientUpdateSubscriber), new { ConnectionString });
            }
        }
        #endregion Public Methods

        #region Private Methods
        private void ClientUpdateReceived(object sender, NetMQSocketEventArgs args)
        {
            string topic = string.Empty;
            string message = string.Empty;

            try
            {
                topic = args.Socket?.ReceiveFrameString();
                message = args.Socket?.ReceiveFrameString();

                if (string.IsNullOrWhiteSpace(topic) is false && message.IsValidJson() is true)
                {
                    if (_subscribedTopicTypeLookup.TryGetValue(topic, out ClientUpdateType updateType) is true)
                    {
                        switch (updateType)
                        {
                            case ClientUpdateType.Queue_Update:
                            {
                                var data = JsonConvert.DeserializeObject<IEnumerable<EncodingJobData>>(message, CommunicationConstants.SerializerSettings);
                                QueueUpdateReceived?.Invoke(this, data);
                                break;
                            }
                            case ClientUpdateType.Status_Update:
                            {
                                var data = JsonConvert.DeserializeObject<EncodingJobStatusUpdateData>(message, CommunicationConstants.SerializerSettings);
                                StatusUpdateReceived?.Invoke(this, data);
                                break;
                            }
                            case ClientUpdateType.Processing_Data_Update:
                            {
                                var data = JsonConvert.DeserializeObject<EncodingJobProcessingDataUpdateData>(message, CommunicationConstants.SerializerSettings);
                                ProcessingDataUpdateReceived?.Invoke(this, data);
                                break;
                            }
                            case ClientUpdateType.Encoding_Progress_Update:
                            {
                                var data = JsonConvert.DeserializeObject<EncodingJobEncodingProgressUpdateData>(message, CommunicationConstants.SerializerSettings);
                                EncodingProgressUpdateReceived?.Invoke(this, data);
                                break;
                            }
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Unknown or not subscribed topic received: {topic}", nameof(ClientUpdateSubscriber));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error with Client Update", nameof(ClientUpdateSubscriber), new { topic, message, SubscribedTopics = _subscribedTopicTypeLookup.Keys, ConnectionString });
            }
        }

        public void Dispose() => Stop();
        #endregion PrivateMethods
    }
}
