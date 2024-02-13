using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Interfaces;
using AutoEncodeUtilities.Logger;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AutoEncodeClient.Communication
{
    public class ClientUpdateSubscriber : IClientUpdateSubscriber, IDisposable
    {
        private enum ClientUpdateType
        {
            None = 0,
            Queue_Update = 1,
            Status_Update = 2,
            Processing_Data_Update = 3,
            Encoding_Progress_Update = 4,
        }

        #region Dependencies
        public ILogger Logger { get; set; }
        #endregion Dependencies

        #region Private Properties
        private bool _initialized = false;
        private readonly SubscriberSocket _subscriberSocket = null;
        private readonly NetMQPoller _poller = null;
        private readonly NetMQMonitor _monitor = null;

        private readonly Dictionary<string, ClientUpdateType> _topicMapper = [];
        private readonly Dictionary<string, Action<EncodingJobQueueUpdateData>> _jobQueueUpdateCallbacks = [];
        private readonly Dictionary<string, Action<EncodingJobStatusUpdateData>> _jobStatusUpdateCallbacks = [];
        private readonly Dictionary<string, Action<EncodingJobProcessingDataUpdateData>> _jobProcessingDataUpdateCallbacks = [];
        private readonly Dictionary<string, Action<EncodingJobEncodingProgressUpdateData>> _jobEncodingProgressUpdateCallbacks = [];
        #endregion Private Properties

        #region Public Properties
        public string ConnectionString => $"tcp://{IpAddress}:{Port}";

        public string IpAddress { get; set; }

        public int Port { get; set; }
        #endregion Public Properties

        public ClientUpdateSubscriber()
        {
            _subscriberSocket = new SubscriberSocket();
            _poller = [_subscriberSocket];

            _subscriberSocket.ReceiveReady += ClientUpdateReceived;

            _monitor = new NetMQMonitor(_subscriberSocket, "inproc://rep.inproc", SocketEvents.Connected | SocketEvents.Disconnected);
            _monitor.Connected += (s, e) => SocketConnected(e);
            _monitor.Disconnected += (s, e) => SocketDisconnected(e);
        }

        #region Public Methods
        public void Initialize(string ipAddress, int port)
        {
            if (_initialized is false)
            {
                IpAddress = ipAddress;
                Port = port;

                _initialized = true;
            }
        }

        public void Start()
        {
            if (_initialized is false) throw new Exception($"{nameof(ClientUpdateSubscriber)} is not initialized.");

            try
            {
                _monitor.StartAsync();

                _subscriberSocket.Connect(ConnectionString);

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

                if (_topicMapper.Count > 0 ) 
                {
                    IEnumerable<string> topics = [.. _topicMapper.Keys];

                    foreach (string topic in topics) 
                    {
                        Unsubscribe(topic);
                    }
                }

                _subscriberSocket.Disconnect(ConnectionString);

                _subscriberSocket.Close();

                _monitor.Stop();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error stopping {nameof(ClientUpdateSubscriber)}", nameof(ClientUpdateSubscriber), new { ConnectionString });
            }
        }

        public bool SubscribeToEncodingJobQueueUpdate(string topic, Action<EncodingJobQueueUpdateData> dataReceivedCallback)
        {
            bool subscribed = false;

            if (_topicMapper.TryAdd(topic, ClientUpdateType.Queue_Update) is true && _jobQueueUpdateCallbacks.TryAdd(topic, dataReceivedCallback) is true)
            {
                _subscriberSocket.Subscribe(topic);
                subscribed = true;
            }
            else
            {
                _topicMapper.Remove(topic);
                _jobQueueUpdateCallbacks.Remove(topic);
            }

            return subscribed;
        }

        public bool SubscribeToEncodingJobStatusUpdate(string topic, Action<EncodingJobStatusUpdateData> dataReceivedCallback)
        {
            bool subscribed = false;

            if (_topicMapper.TryAdd(topic, ClientUpdateType.Status_Update) is true && _jobStatusUpdateCallbacks.TryAdd(topic, dataReceivedCallback) is true)
            {
                _subscriberSocket.Subscribe(topic);
                subscribed = true;
            }
            else
            {
                _topicMapper.Remove(topic);
                _jobStatusUpdateCallbacks.Remove(topic);
            }

            return subscribed;
        }

        public bool SubscribeToEncodingJobProcessingDataUpdate(string topic, Action<EncodingJobProcessingDataUpdateData> dataReceivedCallback)
        {
            bool subscribed = false;

            if (_topicMapper.TryAdd(topic, ClientUpdateType.Processing_Data_Update) is true && _jobProcessingDataUpdateCallbacks.TryAdd(topic, dataReceivedCallback) is true)
            {
                _subscriberSocket.Subscribe(topic);
                subscribed = true;
            }
            else
            {
                _topicMapper.Remove(topic);
                _jobProcessingDataUpdateCallbacks.Remove(topic);
            }

            return subscribed;
        }

        public bool SubscribeToEncodingJobEncodingProgressUpdate(string topic, Action<EncodingJobEncodingProgressUpdateData> dataReceivedCallback)
        {
            bool subscribed = false;

            if (_topicMapper.TryAdd(topic, ClientUpdateType.Encoding_Progress_Update) is true && _jobEncodingProgressUpdateCallbacks.TryAdd(topic, dataReceivedCallback) is true) 
            {
                _subscriberSocket.Subscribe(topic);
                subscribed = true;
            }
            else
            {
                _topicMapper.Remove(topic);
                _jobEncodingProgressUpdateCallbacks.Remove(topic);
            }

            return subscribed;
        }
            
        public bool Unsubscribe(string topic) 
        {
            bool unsubscribed = false;

            if (_topicMapper.TryGetValue(topic, out ClientUpdateType type) is true) 
            {
                _subscriberSocket.Unsubscribe(topic);

                switch (type)
                {
                    case ClientUpdateType.Queue_Update:
                    {
                        _jobQueueUpdateCallbacks.Remove(topic);
                        break;
                    }
                    case ClientUpdateType.Status_Update:
                    {
                        _jobStatusUpdateCallbacks.Remove(topic);
                        break;
                    }
                    case ClientUpdateType.Processing_Data_Update:
                    {
                        _jobProcessingDataUpdateCallbacks.Remove(topic);
                        break;
                    }
                    case ClientUpdateType.Encoding_Progress_Update:
                    {
                        _jobEncodingProgressUpdateCallbacks.Remove(topic);
                        break;
                    }
                }

                _topicMapper.Remove(topic);

                unsubscribed = true;
            }

            return unsubscribed;
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
                    if (_topicMapper.TryGetValue(topic, out ClientUpdateType updateType) is true)
                    {
                        switch (updateType)
                        {
                            case ClientUpdateType.Queue_Update:
                            {
                                var data = JsonConvert.DeserializeObject<EncodingJobQueueUpdateData>(message, CommunicationConstants.SerializerSettings);
                                _jobQueueUpdateCallbacks.TryGetValue(topic, out Action<EncodingJobQueueUpdateData> callback);
                                callback(data);
                                break;
                            }
                            case ClientUpdateType.Status_Update:
                            {
                                var data = JsonConvert.DeserializeObject<EncodingJobStatusUpdateData>(message, CommunicationConstants.SerializerSettings);
                                _jobStatusUpdateCallbacks.TryGetValue(topic, out Action<EncodingJobStatusUpdateData> callback);
                                callback(data);
                                break;
                            }
                            case ClientUpdateType.Processing_Data_Update:
                            {
                                var data = JsonConvert.DeserializeObject<EncodingJobProcessingDataUpdateData>(message, CommunicationConstants.SerializerSettings);
                                _jobProcessingDataUpdateCallbacks.TryGetValue(topic, out Action<EncodingJobProcessingDataUpdateData> callback);
                                callback(data);
                                break;
                            }
                            case ClientUpdateType.Encoding_Progress_Update:
                            {
                                var data = JsonConvert.DeserializeObject<EncodingJobEncodingProgressUpdateData>(message, CommunicationConstants.SerializerSettings);
                                _jobEncodingProgressUpdateCallbacks.TryGetValue(topic, out Action<EncodingJobEncodingProgressUpdateData> callback);
                                callback(data);
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
                Logger.LogException(ex, "Error with Client Update", nameof(ClientUpdateSubscriber), new { topic, message, SubscriptionTopics = _topicMapper.Keys, ConnectionString });
            }
        }

        private void SocketConnected(NetMQMonitorSocketEventArgs e) => Debug.WriteLine($"Connected to {e.Address}");

        private void SocketDisconnected(NetMQMonitorSocketEventArgs e) => Debug.WriteLine($"Disconnected from {e.Address}");

        public void Dispose() => Stop();
        #endregion PrivateMethods
    }
}
