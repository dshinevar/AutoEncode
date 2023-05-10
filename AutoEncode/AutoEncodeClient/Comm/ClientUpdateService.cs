using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoEncodeClient.Comm
{
    public class ClientUpdateService : IDisposable
    {
        private readonly SubscriberSocket SubscriberSocket = null;
        private readonly NetMQPoller Poller = null;
        private readonly ILogger Logger = null;
        private readonly string ConnectionString = string.Empty;
        public event EventHandler<List<EncodingJobData>> DataReceived;

#if DEBUG
        /// <summary>Quick Debug Constructor where IP is localhost</summary>
        /// <param name="logger"></param>
        public ClientUpdateService(ILogger logger) : this(logger, "127.0.0.1", 39000) { }
#endif

        public ClientUpdateService(ILogger logger, string ipAddress, int port)
        {
            ConnectionString = $"tcp://{ipAddress}:{port}";
            Logger = logger;
            SubscriberSocket = new SubscriberSocket();
            Poller = new NetMQPoller { SubscriberSocket };

            SubscriberSocket.ReceiveReady += (s, a) =>
            {
                string topic = string.Empty;
                string message = string.Empty;

                try
                {
                    topic = SubscriberSocket?.ReceiveFrameString();
                    message = SubscriberSocket?.ReceiveFrameString();

                    if (string.IsNullOrWhiteSpace(message) is false && message.IsValidJson() is true)
                    {
                        List<EncodingJobData> clientUpdateData = JsonConvert.DeserializeObject<List<EncodingJobData>>(message, CommunicationConstants.SerializerSettings);

                        if (clientUpdateData is not null)
                        {
                            Task.Factory.StartNew(() => DataReceived?.Invoke(this, clientUpdateData));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Error with Client Update", "ClientUpdateService", new { topic, message, ConnectionString });
                }
            };
        }

        public void Start()
        {
            try
            {
                SubscriberSocket.Connect(ConnectionString);
                SubscriberSocket.Subscribe(CommunicationConstants.ClientUpdateTopic);
                Poller.RunAsync();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to Start ClientUpdateService", nameof(ClientUpdateService), new { ConnectionString });
            }
        }

        public void Stop()
        {
            try
            {
                Poller.Stop();
                Poller.Dispose();
                SubscriberSocket.Unsubscribe(CommunicationConstants.ClientUpdateTopic);
                SubscriberSocket.Close();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to Stop ClientUpdateService", nameof(ClientUpdateService));
            }
        }

        public void Dispose() => Stop();
    }
}
