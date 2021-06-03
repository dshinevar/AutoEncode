using AutomatedFFmpegClient.ClientSocket;
using AutomatedFFmpegUtilities.Messages;
using AutomatedFFmpegClient.Config;
using AutomatedFFmpegUtilities.Base;
using System;
using AutomatedFFmpegClient.Model;
using AutomatedFFmpegUtilities.Enums;
using System.Collections.Generic;
using AutomatedFFmpegUtilities.Data;

namespace AutomatedFFmpegClient
{
    public class AFClientMainThread : AFMainThreadBase
    {
        private MainWindow _mainWindow { get; set; }
        private AFClientSocket _clientSocket { get; set; }
        private AFClientConfig _clientConfig { get; set; }

        /// <summary> Constructor </summary>
        /// <param name="wnd">MainWindow handle</param>
        /// <param name="config">Client config</param>
        public AFClientMainThread(MainWindow wnd, AFClientConfig config) : base(1000)
        {
            _mainWindow = wnd;
            _clientConfig = config;
        }

        #region PUBLIC FUNCTIONS
        /// <summary> Starts AFClientMainThread; Client socket tries to connect. </summary>
        public override void Start()
        {
            _clientSocket = new AFClientSocket(this, _clientConfig.ServerIP, _clientConfig.Port);
            _clientSocket.Connect();
            base.Start();
        }
        /// <summary>Shuts down AFClientMainThread; Closes socket. </summary>
        public override void Shutdown()
        {
            _clientSocket.Close();
            base.Shutdown();
        }
        public void AddProcessMessage(AFMessageBase msg) => AddTask(new Action(() => ProcessMessage(msg)));
        public void Connect() => _clientSocket.Connect();
        public void Disconnect() => _clientSocket.Disconnect();
        public void Send(AFMessageBase msg) => _clientSocket.Send(msg);
        #endregion PUBLIC FUNCTIONS

        protected override void OnTimerElapsed(object obj) => base.OnTimerElapsed(obj);

        #region PRIVATE FUNCTIONS
        private void ProcessMessage(AFMessageBase msg)
        {
            switch (msg.MessageType)
            {
                case AFMessageType.CLIENT_UPDATE:
                {
                    return;
                }
                case AFMessageType.CLIENT_CONNECT:
                {
                    List<VideoSourceViewModel> models = new List<VideoSourceViewModel>();
                    foreach (KeyValuePair<string, List<VideoSourceData>> data in ((ClientConnectMessage)msg).Data.VideoSourceFiles)
                    {
                        VideoSourceViewModel model = new VideoSourceViewModel(data.Key, data.Value);
                        models.Add(model);
                    }
                    _mainWindow.UpdateSource(models);
                    return;
                }
                default:
                {
                    return;
                }
            }
        }
        #endregion PRIVATE FUNCTIONS




    }
}
