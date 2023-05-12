using AutoEncodeUtilities;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Messages;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;

namespace AutoEncodeClient.Comm
{
    public partial class CommunicationManager : IDisposable
    {
        private readonly ILogger Logger = null;
        private readonly RequestSocket RequestSocket = null;
        private readonly string ConnectionString = string.Empty;
        private readonly NetMQMonitor Monitor = null;

        public bool Connected { get; private set; }

        public CommunicationManager(ILogger logger, string ipAddress, int port)
        {
            Logger = logger;
            ConnectionString = $"tcp://{ipAddress}:{port}";

            RequestSocket = new RequestSocket();

            Monitor = new NetMQMonitor(RequestSocket, $"inproc://req.inproc", SocketEvents.Connected | SocketEvents.Disconnected);
            Monitor.Connected += Socket_Connected;
            Monitor.Disconnected += Socket_Disconnected;
            Monitor.StartAsync();

            RequestSocket.Connect(ConnectionString);
        }

        public void Dispose()
        {
            RequestSocket?.Disconnect(ConnectionString);
            RequestSocket?.Close();
            Monitor.Stop();
            Monitor.Dispose();
        }

        #region Socket Events
        private void Socket_Connected(object sender, NetMQMonitorSocketEventArgs e)
        {
            Connected = true;
            Logger.LogInfo($"Connected to {e.Address}", nameof(CommunicationManager));
        }

        private void Socket_Disconnected(object sender, NetMQMonitorSocketEventArgs e)
        {
            Connected = false;
            Logger.LogInfo($"Disconnected from {e.Address}", nameof(CommunicationManager));
        }
        #endregion Socket Events

        #region Private Functions
        private AEMessage<T> SendReceive<T>(AEMessage request)
        {
            AEMessage<T> messageResponse = null;

            string serializedRequest = JsonConvert.SerializeObject(request, CommunicationConstants.SerializerSettings);

            if (string.IsNullOrWhiteSpace(serializedRequest) is false)
            {
                RequestSocket.SendFrame(serializedRequest);

                string response = RequestSocket.ReceiveFrameString();

                if (string.IsNullOrWhiteSpace(response) is false && response.IsValidJson())
                {
                    messageResponse = JsonConvert.DeserializeObject<AEMessage<T>>(response, CommunicationConstants.SerializerSettings);
                }
            }

            return messageResponse;
        }
        #endregion Private Functions
    }
}
