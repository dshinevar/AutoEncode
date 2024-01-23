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
        private readonly ILogger Logger;
        private readonly AEServerMainThread MainThread;
        private readonly RouterSocket RouterSocket = null;
        private readonly NetMQPoller Poller = null;
        private readonly int Port;

        public CommunicationManager(AEServerMainThread mainThread, ILogger logger, int port = 39001)
        {
            MainThread = mainThread;
            Logger = logger;
            Port = port;
            RouterSocket = new RouterSocket();
            Poller = new NetMQPoller { RouterSocket };
            RouterSocket.ReceiveReady += RouterSocket_ReceiveReady;
        }

        public void Start()
        {
            try
            {
                Logger.LogInfo($"Binding to *:{Port}", nameof(CommunicationManager));
                RouterSocket.Bind($"tcp://*:{Port}");
                Poller.RunAsync();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error Starting Comm Manager", nameof(CommunicationManager), new { Port });
            }
        }

        public void Stop()
        {
            try
            {
                Logger.LogInfo("Stopping Comm Manager", nameof(CommunicationManager));
                Poller.Stop();
                Poller.Dispose();
                RouterSocket.Close();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error Stopping Comm Manager", nameof(CommunicationManager), new { Port });
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
                Logger.LogException(ex, "Error handling received message.", nameof(CommunicationManager), new { Port });
            }
        }

        private void SendMessage<T>(NetMQFrame clientAddress, T obj)
        {
            NetMQMessage message = new();
            message.Append(clientAddress);
            message.AppendEmptyFrame();

            var response = JsonConvert.SerializeObject(obj, CommunicationConstants.SerializerSettings);

            message.Append(response);

            RouterSocket.SendMultipartMessage(message);
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
                        var (Movies, Shows) = MainThread.RequestSourceFiles();
                        var response = AEMessageFactory.CreateSourceFilesResponse(Movies, Shows);
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
                        EncodeRequest request = ((AEMessage<EncodeRequest>)aeMessage).Data;
                        bool success = MainThread.RequestEncodingJob(request.Guid, request.IsShow);
                        SendMessage(clientAddress, AEMessageFactory.CreateEncodeResponse(success));
                        break;
                    }
                }
            }
        }
    }
}
