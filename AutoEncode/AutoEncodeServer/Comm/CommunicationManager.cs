using AutoEncodeUtilities;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Messages;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;

namespace AutoEncodeServer.Comm
{
    public class CommunicationManager
    {
        private readonly ILogger Logger;
        private readonly AEServerMainThread MainThread;
        private readonly ResponseSocket ResponseSocket = null;
        private readonly NetMQPoller Poller = null;
        private readonly int Port;

        public CommunicationManager(AEServerMainThread mainThread, ILogger logger, int port = 39001)
        {
            MainThread = mainThread;
            Logger = logger;
            Port = port;
            ResponseSocket = new ResponseSocket();
            Poller = new NetMQPoller { ResponseSocket };

            ResponseSocket.ReceiveReady += (s, a) =>
            {
                try
                {
                    while (a.Socket.TryReceiveFrameString(out string message))
                    {
                        if (string.IsNullOrWhiteSpace(message) is false && message.IsValidJson())
                        {
                            ProcessMessage(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Error handling received message.", nameof(CommunicationManager), new { Port });
                }
            };
        }

        public void Start()
        {
            try
            {
                Logger.LogInfo($"Binding to *:{Port}", nameof(CommunicationManager));
                ResponseSocket.Bind($"tcp://*:{Port}");
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
                ResponseSocket.Close();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error Stopping Comm Manager", nameof(CommunicationManager), new { Port });
            }
        }

        private void ProcessMessage(string message)
        {
            AEMessage aeMessage = JsonConvert.DeserializeObject<AEMessage>(message, CommunicationConstants.SerializerSettings);

            if (aeMessage is not null)
            {
                switch (aeMessage.MessageType)
                {
                    case AEMessageType.Status_MovieSourceFiles_Request:
                    {
                        var response = AEMessageFactory.CreateMovieSourceFilesResponse(MainThread.GetMovieSourceData());
                        ResponseSocket.SendFrame(JsonConvert.SerializeObject(response, CommunicationConstants.SerializerSettings));
                        break;
                    }
                    case AEMessageType.Status_ShowSourceFiles_Request:
                    {
                        var response = AEMessageFactory.CreateShowSourceFilesResponse(MainThread.GetShowSourceData());
                        ResponseSocket.SendFrame(JsonConvert.SerializeObject(response, CommunicationConstants.SerializerSettings));
                        break;
                    }
                    case AEMessageType.Cancel_Request:
                    {
                        bool success = EncodingJobQueue.CancelJob(((AEMessage<ulong>)aeMessage).Data);
                        var response = AEMessageFactory.CreateCancelResponse(success);
                        ResponseSocket.SendFrame(JsonConvert.SerializeObject(response, CommunicationConstants.SerializerSettings));
                        break;
                    }
                    case AEMessageType.Pause_Request:
                    {
                        bool success = EncodingJobQueue.PauseJob(((AEMessage<ulong>)aeMessage).Data);
                        var response = AEMessageFactory.CreatePauseResponse(success);
                        ResponseSocket.SendFrame(JsonConvert.SerializeObject(response, CommunicationConstants.SerializerSettings));
                        break;
                    }
                    case AEMessageType.Resume_Request:
                    {
                        bool success = EncodingJobQueue.ResumeJob(((AEMessage<ulong>)aeMessage).Data);
                        var response = AEMessageFactory.CreateResumeResponse(success);
                        ResponseSocket.SendFrame(JsonConvert.SerializeObject(response, CommunicationConstants.SerializerSettings));
                        break;
                    }
                    case AEMessageType.Cancel_Pause_Request:
                    {
                        bool success = EncodingJobQueue.CancelThenPauseJob(((AEMessage<ulong>)aeMessage).Data);
                        var response = AEMessageFactory.CreateCancelPauseResponse(success);
                        ResponseSocket.SendFrame(JsonConvert.SerializeObject(response, CommunicationConstants.SerializerSettings));
                        break;
                    }
                }
            }
        }
    }
}
