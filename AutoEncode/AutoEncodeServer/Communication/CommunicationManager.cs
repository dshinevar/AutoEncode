using AutoEncodeServer.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Messages;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;

namespace AutoEncodeServer.Communication
{
    public class CommunicationManager : ICommunicationManager
    {
        #region Private Properties
        private readonly RouterSocket _routerSocket = null;
        private readonly NetMQPoller _poller = null;
        #endregion Private Properties

        #region Dependencies
        public ILogger Logger { get; set; }
        #endregion Dependencies

        #region Public Properties
        public string ConnectionString => $"tcp://*:{Port}";
        public int Port { get; private set; }

        public event EventHandler<AEMessageReceivedArgs> MessageReceived;
        #endregion Public Properties

        public CommunicationManager()
        {
            _routerSocket = new RouterSocket();
            _poller = [_routerSocket];
            _routerSocket.ReceiveReady += RouterSocket_ReceiveReady;
        }

        public bool Start(int port)
        {
            try
            {
                Port = port;

                Logger.LogInfo($"Binding to *:{Port}", nameof(CommunicationManager));
                _routerSocket.Bind(ConnectionString);
                _poller.RunAsync();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error Starting Comm Manager", nameof(CommunicationManager), new { Port });
                return false;
            }

            return true;
        }

        public bool Stop()
        {
            try
            {
                Logger.LogInfo("Stopping Comm Manager", nameof(CommunicationManager));
                _poller.Stop();
                _poller.Dispose();
                _routerSocket.Close();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error Stopping Comm Manager", nameof(CommunicationManager), new { Port });
                return false;
            }

            return true;
        }

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
                            AEMessage aeMessage = JsonConvert.DeserializeObject<AEMessage>(messageString, CommunicationConstants.SerializerSettings);

                            MessageReceived?.Invoke(this, new AEMessageReceivedArgs(message[0], aeMessage));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error handling received message.", nameof(CommunicationManager), new { Port });
            }
        }

        public void SendMessage<T>(NetMQFrame clientAddress, T obj)
        {
            NetMQMessage message = new();
            message.Append(clientAddress);
            message.AppendEmptyFrame();

            var response = JsonConvert.SerializeObject(obj, CommunicationConstants.SerializerSettings);

            message.Append(response);

            _routerSocket.SendMultipartMessage(message);
        }
    }
}
