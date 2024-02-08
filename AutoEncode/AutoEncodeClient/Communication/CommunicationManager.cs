using AutoEncodeUtilities;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Messages;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeClient.Communication
{
    public partial class CommunicationManager : ICommunicationManager
    {
        #region Dependencies
        public ILogger Logger { get; set; }
        #endregion Dependencies

        #region Public Properties
        public string ConnectionString => $"tcp://{IpAddress}:{Port}";
        public string IpAddress { get; set; }
        public int Port { get; set; }
        #endregion Public Properties

        /// <summary>Default Constructor</summary>
        public CommunicationManager() { }

        public void Initialize(string ipAddress, int port) 
        {
            IpAddress = ipAddress;
            Port = port;
        }

        #region Private Functions
        private async Task<T> SendReceive<T>(AEMessage message, AEMessageType expectedResponseType)
        {
            var response = await SendReceiveAsync<T>(message);
            return HandleResponseMessage(response, expectedResponseType);
        }

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

                return messageResponse ?? new AEMessage<T>(AEMessageType.Error, default);

            }, TaskCreationOptions.LongRunning);
        }

        private T HandleResponseMessage<T>(AEMessage<T> message, AEMessageType expectedMessageType)
        {
            if (message is null)
            {
                throw new Exception("Null response message received.");
            }

            if (message.MessageType.Equals(AEMessageType.Error))
            {
                throw new Exception("Error occurred with response message.");
            }

            if (!message.MessageType.Equals(expectedMessageType))
            {
                throw new Exception($"Response message has unexpected message type (Expected: {expectedMessageType} | Received {message.MessageType})");
            }

            return message.Data;
        }

        #endregion Private Functions
    }
}
