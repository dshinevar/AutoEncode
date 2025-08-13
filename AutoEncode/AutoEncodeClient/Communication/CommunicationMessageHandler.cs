using AutoEncodeClient.Communication.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Communication;
using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using AutoEncodeUtilities.Logger;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeClient.Communication;

public partial class CommunicationMessageHandler : ICommunicationMessageHandler
{
    #region Dependencies
    public ILogger Logger { get; set; }
    #endregion Dependencies

    private bool _initialized = false;
    private CancellationTokenSource _shutdownCancellationTokenSource = new();

    public string ConnectionString => $"tcp://{IpAddress}:{Port}";
    public string IpAddress { get; set; }
    public int Port { get; set; }

    /// <summary>Default Constructor </summary>
    public CommunicationMessageHandler() { }

    public void Initialize()
    {
        if (_initialized is false)
        {
            IpAddress = State.ConnectionSettings.IPAddress;
            Port = State.ConnectionSettings.CommunicationPort;
        }

        _initialized = true;
    }

    public void Shutdown()
    {
        _shutdownCancellationTokenSource.Cancel();
    }

    private async Task<CommunicationMessage<ResponseMessageType>> SendReceiveAsync(CommunicationMessage<RequestMessageType> message)
    {
        if (_initialized is false)
        {
            throw new InvalidOperationException($"{nameof(CommunicationMessageHandler)} not initialized.");
        }

        CommunicationMessage<ResponseMessageType> responseMessage = null;

        responseMessage = await Task.Run(() =>
        {
            CancellationToken token = _shutdownCancellationTokenSource.Token;

            try
            {
                using DealerSocket client = new(ConnectionString);
                client.Options.Identity = Encoding.ASCII.GetBytes(Guid.NewGuid().ToString());

                string serializedRequest = JsonSerializer.Serialize(message, CommunicationConstants.SerializerOptions);

                if (string.IsNullOrWhiteSpace(serializedRequest) is false)
                {
                    token.ThrowIfCancellationRequested();

                    NetMQMessage netMqMessage = new();
                    netMqMessage.AppendEmptyFrame();
                    netMqMessage.Append(serializedRequest);

                    NetMQMessage response = null;
                    try
                    {
                        client.SendMultipartMessage(netMqMessage);

                        response = client.ReceiveMultipartMessage();
                    }
                    catch (TerminatingException) { }

                    token.ThrowIfCancellationRequested();

                    if (response?.FrameCount == 2)
                    {
                        string responseString = response[1].ConvertToString();

                        if (responseString.IsValidJson())
                        {
                            return JsonSerializer.Deserialize<CommunicationMessage<ResponseMessageType>>(responseString, CommunicationConstants.SerializerOptions);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }

            return new CommunicationMessage<ResponseMessageType>(ResponseMessageType.Error);
        }, _shutdownCancellationTokenSource.Token);

        return responseMessage;
    }

    /// <summary>
    /// Validates the given response message to ensure the correct response type.<br/>
    /// Throws an exception if validation fails:<br/>
    /// 1. If response is null.<br/>
    /// 2. Response message type is <see cref="CommunicationMessageType.Error"/><br/>
    /// 3. Reponse message type does not match expected.
    /// </summary>
    /// <param name="response">The response <see cref="CommunicationMessage"/></param>
    /// <param name="expectedResponseType">The expected <see cref="CommunicationMessageType"/> from the response.</param>
    /// <exception cref="Exception"/>
    private static void ValidateResponse(CommunicationMessage<ResponseMessageType> response, ResponseMessageType expectedResponseType)
    {
        if (response is null)
        {
            throw new Exception("Null response message received.");
        }
        else if (response.Type == ResponseMessageType.Error)
        {
            throw new Exception("Error occurred with response message.");
        }
        else if (response.Type != expectedResponseType)
        {
            throw new Exception("Invalid response message type.");
        }
    }
}
