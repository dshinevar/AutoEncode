using AutoEncodeServer.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Logger;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;

namespace AutoEncodeServer.Communication
{
    public class ClientUpdatePublisher : IClientUpdatePublisher
    {
        #region Dependencies
        public ILogger Logger { get; set; }
        #endregion Dependencies

        #region Private Properties
        private readonly object _lock = new();
        private bool _initialized = false;
        private readonly PublisherSocket _publisherSocket = null;
        #endregion Private Properties

        #region Properties
        public string ConnectionString => $"tcp://*:{Port}";
        public int Port { get; set; }
        #endregion Properties

        /// <summary>Default Constructor</summary>
        public ClientUpdatePublisher()
        {
            _publisherSocket = new PublisherSocket();
        }

        public void Initialize(int port)
        {
            if (_initialized is false)
            {
                Port = port;

                _initialized = true;
            }
        }

        public void Start()
        {
            if (_initialized is false) throw new Exception($"{nameof(ClientUpdatePublisher)} is not initialized.");

            try
            {
                _publisherSocket.Bind(ConnectionString);
                Logger.LogInfo($"Binding to {ConnectionString}.", nameof(ClientUpdatePublisher));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error Starting {nameof(ClientUpdatePublisher)}", nameof(ClientUpdatePublisher), new { Port });
                throw;
            }
        }

        public void Shutdown()
        {
            try
            {
                _publisherSocket?.Unbind(ConnectionString);
                _publisherSocket?.Close();
                _publisherSocket?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error Shutting Down {nameof(ClientUpdatePublisher)}", nameof(ClientUpdatePublisher));
                throw;
            }
        }

        public void SendUpdateToClients(string topic, object data)
        {
            if (_publisherSocket.IsDisposed is true) return;

            try
            {
                string serializedData = JsonConvert.SerializeObject(data, CommunicationConstants.SerializerSettings);

                if (string.IsNullOrWhiteSpace(serializedData) is false)
                {
                    lock (_lock)
                    {
                        _publisherSocket.SendMoreFrame(topic).SendFrame(serializedData);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error Sending Update To Clients", nameof(ClientUpdatePublisher), new { Port, topic });
            }
        }
    }
}
