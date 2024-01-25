using AutoEncodeUtilities;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Messages;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace AutoEncodeServer.Comm
{
    public class CommunicationManager
    {
        #region Private Properties
        private readonly ILogger _logger;
        private readonly AEServerMainThread _mainThread;
        private readonly RouterSocket _routerSocket = null;
        private readonly NetMQPoller _poller = null;
        #endregion Private Properties

        public string ConnectionString => $"tcp://*:{Port}";
        public int Port { get; }

        public CommunicationManager(AEServerMainThread mainThread, ILogger logger, int port = 39001)
        {
            _mainThread = mainThread;
            _logger = logger;
            Port = port;

            _routerSocket = new RouterSocket();
            _poller = new NetMQPoller { _routerSocket };
            _routerSocket.ReceiveReady += RouterSocket_ReceiveReady;
        }

        public void Start()
        {
            try
            {
                _logger.LogInfo($"Binding to *:{Port}", nameof(CommunicationManager));
                _routerSocket.Bind(ConnectionString);
                _poller.RunAsync();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error Starting Comm Manager", nameof(CommunicationManager), new { Port });
            }
        }

        public void Stop()
        {
            try
            {
                _logger.LogInfo("Stopping Comm Manager", nameof(CommunicationManager));
                _poller.Stop();
                _poller.Dispose();
                _routerSocket.Close();
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error Stopping Comm Manager", nameof(CommunicationManager), new { Port });
            }
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
                            Task.Factory.StartNew(() => ProcessMessage(message[0], messageString));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "Error handling received message.", nameof(CommunicationManager), new { Port });
            }
        }

        private void SendMessage<T>(NetMQFrame clientAddress, T obj)
        {
            NetMQMessage message = new();
            message.Append(clientAddress);
            message.AppendEmptyFrame();

            var response = JsonConvert.SerializeObject(obj, CommunicationConstants.SerializerSettings);

            message.Append(response);

            _routerSocket.SendMultipartMessage(message);
        }

        private void ProcessMessage(NetMQFrame clientAddress, string message)
        {
            AEMessage aeMessage = JsonConvert.DeserializeObject<AEMessage>(message, CommunicationConstants.SerializerSettings);

            if (aeMessage is not null)
            {
                switch (aeMessage.MessageType)
                {
                    case AEMessageType.Source_Files_Request:
                    {
                        var sourceFiles = _mainThread.RequestSourceFiles();
                        var response = AEMessageFactory.CreateSourceFilesResponse(sourceFiles);
                        SendMessage(clientAddress, response);
                        break;
                    }
                    case AEMessageType.Cancel_Request:
                    {
                        bool success = EncodingJobQueue.CancelJob(((AEMessage<ulong>)aeMessage).Data);
                        var response = AEMessageFactory.CreateCancelResponse(success);
                        SendMessage(clientAddress, response);
                        break;
                    }
                    case AEMessageType.Pause_Request:
                    {
                        bool success = EncodingJobQueue.PauseJob(((AEMessage<ulong>)aeMessage).Data);
                        var response = AEMessageFactory.CreatePauseResponse(success);
                        SendMessage(clientAddress, response);
                        break;
                    }
                    case AEMessageType.Resume_Request:
                    {
                        bool success = EncodingJobQueue.ResumeJob(((AEMessage<ulong>)aeMessage).Data);
                        var response = AEMessageFactory.CreateResumeResponse(success);
                        SendMessage(clientAddress, response);
                        break;
                    }
                    case AEMessageType.Cancel_Pause_Request:
                    {
                        bool success = EncodingJobQueue.CancelThenPauseJob(((AEMessage<ulong>)aeMessage).Data);
                        var response = AEMessageFactory.CreateCancelPauseResponse(success);
                        SendMessage(clientAddress, response);
                        break;
                    }
                    case AEMessageType.Encode_Request:
                    {
                        Guid guid = ((AEMessage<Guid>)aeMessage).Data;
                        bool success = _mainThread.RequestEncodingJob(guid);
                        SendMessage(clientAddress, AEMessageFactory.CreateEncodeResponse(success));
                        break;
                    }
                    case AEMessageType.Remove_Job_Request:
                    {
                        ulong jobId = ((AEMessage<ulong>)aeMessage).Data;
                        bool success = EncodingJobQueue.CancelThenPauseJob(jobId);
                        if (success is true) success = EncodingJobQueue.RemoveEncodingJobById(jobId);
                        SendMessage(clientAddress,AEMessageFactory.CreateRemoveJobResponse(success));
                        break;
                    }
                }
            }
        }
    }
}
