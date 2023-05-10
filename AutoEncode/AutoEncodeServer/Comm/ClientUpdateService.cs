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
        private readonly ILogger Logger = null;
        private Timer PublishTimer = null;
        private readonly PublisherSocket PublisherSocket = null;
        private readonly int Port;

        public ClientUpdateService(ILogger logger, int port = 39000)
        {
            Logger = logger;
            Port = port;
            PublisherSocket = new PublisherSocket();
        }

        public bool Initialize()
        {
            bool success = true;
            try
            {
                Logger.LogInfo($"Binding to *:{Port}", nameof(ClientUpdateService));
                PublisherSocket.Bind($"tcp://*:{Port}");
                PublishTimer = new Timer(SendUpdateToClients, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error Initializing ClientUpdateService.", nameof(ClientUpdateService), new { Port });
                success = false;
            }

            return success;
        }

        public void Shutdown()
        {
            try
            {
                Logger.LogInfo($"Shutting down ClientUpdateService", nameof(ClientUpdateService));
                PublishTimer?.Dispose();

                PublisherSocket?.Close();
                PublisherSocket?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error shutting down ClientUpdateService", nameof(ClientUpdateService), new { Port });
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
                    PublisherSocket.SendMoreFrame(CommunicationConstants.ClientUpdateTopic).SendFrame(output);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error Sending Update To Clients", nameof(ClientUpdateService), new { Port });
            }
        }
    }
}
