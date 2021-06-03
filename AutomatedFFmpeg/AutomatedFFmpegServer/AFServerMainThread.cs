using AutomatedFFmpegServer.ServerSocket;
using AutomatedFFmpegUtilities.Base;
using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Messages;
using AutomatedFFmpegUtilities.Messages.ServerToClient;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegServer.Base;
using AutomatedFFmpegServer.WorkerThreads;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomatedFFmpegServer
{
    public class AFServerMainThread : AFMainThreadBase
    {
        private List<AFWorkerThreadBase> _workerThreads { get; set; } = new List<AFWorkerThreadBase>();
        private EncodingJobFinderThread _encodingJobFinderThread { get; set; }
        private AFServerSocket _serverSocket { get; set; }
        private AFServerConfig _config { get; set; }
        private EncodingJobs _encodingJobs = new EncodingJobs();
        private bool _alive { get; set; } = true;

        /// <summary>Constructor; Creates Server Socket</summary>
        /// <param name="serverConfig">Server Config</param>
        public AFServerMainThread(AFServerConfig serverConfig) : base(1000)
        {
            _config = serverConfig;
            _serverSocket = new AFServerSocket(this, _config.ServerSettings.IP, _config.ServerSettings.Port);
            _workerThreads.Add(_encodingJobFinderThread = new EncodingJobFinderThread(this, _config, _encodingJobs));
            _workerThreads.Add(new EncodingJobBuilderThread(this, _config, _encodingJobs));
            _workerThreads.Add(new EncodingThread(this, _config, _encodingJobs));
        }
        #region PUBLIC FUNCTIONS
        /// <summary> Starts AFServerMainThread; Server socket starts listening. </summary>
        public override void Start()
        {
            _serverSocket?.StartListening();
            _workerThreads.ForEach(x => x.Start(null));
            base.Start();
        }
        /// <summary>Shuts down AFServerMainThread; Disconnects server socket. </summary>
        public override void Shutdown()
        {
            _serverSocket.Disconnect(false);
            _workerThreads.ForEach(x => x.Shutdown());
            base.Shutdown();
            _alive = false;
        }
        public bool IsAlive() => _alive;
        /// <summary>Adds ProcessMessage task to Task Queue (Client to Server Message).</summary>
        /// <param name="msg">AFMessageBase</param>
        public void AddProcessMessage(AFMessageBase msg) => AddTask(() => ProcessMessage(msg));
        public void AddSendClientConnectData() => AddTask(() => SendClientConnectData());
        /// <summary>Adds SendMessage task to Task Queue (Server To Client Message). </summary>
        /// <param name="msg">AFMessageBase</param>
        public void AddSendMessage(AFMessageBase msg) => AddTask(() => SendMessage(msg));
        #endregion PUBLIC FUNCTIONS

        /// <summary>Timer task</summary>
        /// <param name="obj">Task Queue</param>
        protected override void OnTimerElapsed(object obj) 
        {
            base.OnTimerElapsed(obj);
            //SendClientUpdate();
        }

        #region PRIVATE FUNCTIONS
        /// <summary>Process received message from client. </summary>
        /// <param name="msg"></param>
        private void ProcessMessage(AFMessageBase msg) => Console.WriteLine($"Message from Client: {msg.MessageType}");
        /// <summary>Send message to client.</summary>
        /// <param name="msg"></param>
        private void SendMessage(AFMessageBase msg) => _serverSocket.Send(msg);

        private void SendClientConnectData()
        {
            ClientConnectData clientConnect = new ClientConnectData()
            {
                VideoSourceFiles = _encodingJobFinderThread.GetVideoSourceFiles(),
                ShowSourceFiles = _encodingJobFinderThread.GetShowSourceFiles()
            };
            SendMessage(ServerToClientMessageFactory.CreateClientConnectMessage(clientConnect));
        }

        private void SendClientUpdate()
        {
            ClientUpdateData clientUpdate = new ClientUpdateData()
            {
                ThreadStatuses = GetThreadStatuses(),
                EncodingJobs = _encodingJobs.GetEncodingJobs()
            };
            SendMessage(ServerToClientMessageFactory.CreateClientUpdateMessage(clientUpdate));
        }

        private List<ThreadStatusData> GetThreadStatuses()
        {
            List<ThreadStatusData> threadStatuses = new List<ThreadStatusData>();
            foreach(AFWorkerThreadBase thread in _workerThreads)
            {
                threadStatuses.Add(thread.GetThreadStatus());
            }
            return threadStatuses;
        }
        #endregion PRIVATE FUNCTIONS
    }
}
