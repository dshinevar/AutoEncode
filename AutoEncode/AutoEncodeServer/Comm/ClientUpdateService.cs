using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AutoEncodeServer.Comm
{
    public class ClientUpdateService
    {
        private readonly ILogger _logger = null;
        private Timer _publishTimer = null;
        private readonly PublisherSocket _publisherSocket = null;

        public int Port { get; }    

        public ClientUpdateService(ILogger logger, int port = 39000)
        {
            _logger = logger;
            Port = port;
            _publisherSocket = new PublisherSocket();
            _publisherSocket.Options.SendHighWatermark = 1;
        }

        public bool Initialize()
        {
            bool success = true;
            try
            {
                _logger.LogInfo($"Binding to *:{Port}", nameof(ClientUpdateService));
                _publisherSocket.Bind($"tcp://*:{Port}");
                _publishTimer = new Timer(SendUpdateToClients, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error Initializing ClientUpdateService.", nameof(ClientUpdateService), new { Port });
                success = false;
            }

            return success;
        }

        public void Shutdown()
        {
            try
            {
                _logger.LogInfo($"Shutting down ClientUpdateService", nameof(ClientUpdateService));
                _publishTimer?.Dispose();

                _publisherSocket?.Close();
                _publisherSocket?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error shutting down ClientUpdateService", nameof(ClientUpdateService), new { Port });
            }
        }

        private void SendUpdateToClients(object state)
        {
            try
            {
                List<EncodingJobData> encodingJobsData = EncodingJobQueue.GetEncodingJobsData();
                string output = JsonConvert.SerializeObject(encodingJobsData, CommunicationConstants.SerializerSettings);

                if (string.IsNullOrWhiteSpace(output) is false)
                {
                    _publisherSocket.SendMoreFrame(CommunicationConstants.ClientUpdateTopic).SendFrame(output);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error Sending Update To Clients", nameof(ClientUpdateService), new { Port });
            }
        }
    }
}
