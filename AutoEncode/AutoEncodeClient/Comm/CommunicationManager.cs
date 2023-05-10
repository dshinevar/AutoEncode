using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Messages;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AutoEncodeClient.Comm
{
    public class CommunicationManager : IDisposable
    {
        private readonly ILogger Logger = null;
        private readonly RequestSocket RequestSocket = null;
        private readonly string ConnectionString = string.Empty;

        public CommunicationManager(ILogger logger, string ipAddress, int port)
        {
            Logger = logger;
            ConnectionString = $"tcp://{ipAddress}:{port}";
            RequestSocket = new RequestSocket();
            RequestSocket.Connect(ConnectionString);
        }

        public void Dispose()
        {
            RequestSocket?.Close();
        }

        public Dictionary<string, List<VideoSourceData>> GetMovieSourceData()
        {
            Dictionary<string, List<VideoSourceData>> returnData = null;

            try
            {
                AEMessage<Dictionary<string, List<VideoSourceData>>> returnMessage = SendReceive<Dictionary<string, List<VideoSourceData>>>(AEMessageFactory.CreateMovieSourceFilesRequest());
                returnData = returnMessage.Data;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to get movie source data.", nameof(CommunicationManager), new { ConnectionString });
            }

            return returnData;
        }

        public Dictionary<string, List<ShowSourceData>> GetShowSourceData()
        {
            Dictionary<string, List<ShowSourceData>> returnData = null;

            try
            {
                AEMessage<Dictionary<string, List<ShowSourceData>>> returnMessage = SendReceive<Dictionary<string, List<ShowSourceData>>>(AEMessageFactory.CreateShowSourceFilesRequest());
                returnData = returnMessage.Data;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to get show source data.", nameof(CommunicationManager), new { ConnectionString });
            }

            return returnData;
        }

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
    }
}
