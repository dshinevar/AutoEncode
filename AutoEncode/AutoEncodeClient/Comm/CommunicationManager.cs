using AutoEncodeUtilities;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Messages;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeClient.Comm
{
    public partial class CommunicationManager
    {
        private readonly ILogger Logger = null;
        private readonly string ConnectionString = string.Empty;

        public CommunicationManager(ILogger logger, string ipAddress, int port)
        {
            Logger = logger;
            ConnectionString = $"tcp://{ipAddress}:{port}";
        }

        #region Private Functions
        private async Task<AEMessage<T>> SendReceiveAsync<T>(AEMessage request)
        {
            return await Task.Factory.StartNew(() =>
            {
                AEMessage<T> messageResponse = null;

                using (DealerSocket client = new(ConnectionString))
                {
                    client.Options.Identity = Encoding.ASCII.GetBytes(Guid.NewGuid().ToString());

                    string serializedRequest = JsonConvert.SerializeObject(request, CommunicationConstants.SerializerSettings);

                    if (string.IsNullOrWhiteSpace(serializedRequest) is false)
                    {
                        NetMQMessage netMqMessage = new();
                        netMqMessage.AppendEmptyFrame();
                        netMqMessage.Append(serializedRequest);

                        client.SendMultipartMessage(netMqMessage);

                        NetMQMessage response = client.ReceiveMultipartMessage();

                        if (response.FrameCount == 2)
                        {
                            string responseString = response[1].ConvertToString();

                            if (string.IsNullOrWhiteSpace(responseString) is false && responseString.IsValidJson())
                            {
                                messageResponse = JsonConvert.DeserializeObject<AEMessage<T>>(responseString, CommunicationConstants.SerializerSettings);
                            }
                        }
                    }
                }

                return messageResponse;

            }, TaskCreationOptions.LongRunning);

        }

        #endregion Private Functions
    }
}
