using AutomatedFFmpegServer.ServerSocket;
using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Messages;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegServer.Base;
using AutomatedFFmpegServer.WorkerThreads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutomatedFFmpegServer
{
    public class AFServerMainThread : AFMainThreadBase
    {
        private Task EncodingJobBuilderTask { get; set; }
        private CancellationTokenSource EncodingJobBuilderCancellationToken { get; set; } = new CancellationTokenSource();

        private Timer ServerTimer { get ; set; }
        private EncodingJobFinderThread EncodingJobFinderThread { get; set; }
        private AFServerSocket ServerSocket { get; set; }
        private AFServerConfig Config { get; set; }
        private ManualResetEvent ShutdownMRE { get; set; }

        /// <summary>Constructor; Creates Server Socket</summary>
        /// <param name="serverConfig">Server Config</param>
        public AFServerMainThread(AFServerConfig serverConfig, ManualResetEvent shutdown) : base(1000)
        {
            Config = serverConfig;
            ShutdownMRE = shutdown;
            ServerSocket = new AFServerSocket(this, Config.ServerSettings.IP, Config.ServerSettings.Port);
            EncodingJobFinderThread = new EncodingJobFinderThread(this, Config);
        }
        #region PUBLIC FUNCTIONS
        /// <summary> Starts AFServerMainThread; Server socket starts listening. </summary>
        public override void Start()
        {
            base.Start();
            EncodingJobFinderThread.Start(null);
            ServerSocket?.StartListening();
            ServerTimer = new Timer(OnServerTimerElapsed, null, 10000, 1000);
        }
        
        /// <summary>Shuts down AFServerMainThread; Disconnects server socket. </summary>
        public override void Shutdown()
        {
            ServerSocket.Disconnect(false);
            ServerSocket.Dispose();
            base.Shutdown();
            ShutdownMRE.Set();
        }

        /// <summary>Adds ProcessMessage task to Task Queue (Client to Server Message).</summary>
        /// <param name="msg">AFMessageBase</param>
        public void AddProcessMessage(AFMessageBase msg) => AddTask(() => ProcessMessage(msg));
        public void AddSendClientConnectData() => AddTask(() => SendClientConnectData());
        /// <summary>Adds SendMessage task to Task Queue (Server To Client Message). </summary>
        /// <param name="msg">AFMessageBase</param>
        public void AddSendMessage(AFMessageBase msg) => AddTask(() => SendMessage(msg));
        #endregion PUBLIC FUNCTIONS

        /// <summary>Server timer task: Send update to client; Spin up threads for other tasks</summary>
        private void OnServerTimerElapsed(object obj)
        {
            // TODO: Handle Cancelling

            if (EncodingJobQueue.Any())
            {
                // Check if task is done (or null -- first time setup)
                if (EncodingJobBuilderTask?.IsCompletedSuccessfully ?? true)
                {
                    EncodingJob jobToBuild = EncodingJobQueue.GetNextEncodingJobWithStatus(EncodingJobStatus.NEW);
                    if (jobToBuild is not null)
                    {
                        EncodingJobBuilderTask = Task.Factory.StartNew(() 
                            => EncodingJobTasks.BuildEncodingJob(jobToBuild, EncodingJobBuilderCancellationToken.Token), EncodingJobBuilderCancellationToken.Token);
                    }
                }
            }

            
            if (ServerSocket.IsConnected()) SendMessage(ServerToClientMessageFactory.CreateClientUpdateMessage(new ClientUpdateData()));
        }

        #region PRIVATE FUNCTIONS
        /// <summary>Process received message from client. </summary>
        /// <param name="msg"></param>
        private void ProcessMessage(AFMessageBase msg)
        {
            switch (msg.MessageType)
            {
                case AFMessageType.CLIENT_REQUEST:
                {
                    SendClientConnectData();
                    break;
                }
            }
        }
        /// <summary>Send message to client.</summary>
        /// <param name="msg"></param>
        private void SendMessage(AFMessageBase msg) => ServerSocket.Send(msg);

        private void SendClientConnectData()
        {
            ClientConnectData clientConnect = new ClientConnectData()
            {
                VideoSourceFiles = EncodingJobFinderThread.GetVideoSourceFiles(),
                ShowSourceFiles = EncodingJobFinderThread.GetShowSourceFiles()
            };
            SendMessage(ServerToClientMessageFactory.CreateClientConnectMessage(clientConnect));
        }
        #endregion PRIVATE FUNCTIONS
    }
}
