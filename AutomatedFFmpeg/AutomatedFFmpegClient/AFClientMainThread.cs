using AutomatedFFmpegClient.ClientSocket;
using AutomatedFFmpegUtilities.Messages;
using AutomatedFFmpegClient.Config;
using System;
using AutomatedFFmpegClient.ViewData;
using AutomatedFFmpegUtilities.Enums;
using System.Collections.Generic;
using AutomatedFFmpegUtilities.Data;

namespace AutomatedFFmpegClient
{
    public class AFClientMainThread
    {
        private MainWindow _mainWindow { get; set; }
        private AFClientSocket _clientSocket { get; set; }
        private AFClientConfig _clientConfig { get; set; }

        /// <summary> Constructor </summary>
        /// <param name="wnd">MainWindow handle</param>
        /// <param name="config">Client config</param>
        public AFClientMainThread(MainWindow wnd, AFClientConfig config)
        {
            _mainWindow = wnd;
            _clientConfig = config;
        }

        #region PUBLIC FUNCTIONS
        /// <summary> Starts AFClientMainThread; Client socket tries to connect. </summary>
        public  void Start()
        {
            _clientSocket = new AFClientSocket(this, _clientConfig.ServerIP, _clientConfig.Port);
            _clientSocket.Connect();
        }
        /// <summary>Shuts down AFClientMainThread; Closes socket. </summary>
        public void Shutdown()
        {
            _clientSocket.Close();
        }
        //public void AddProcessMessage(AFMessageBase msg) => AddTask(new Action(() => ProcessMessage(msg)));
        public void Connect() => _clientSocket.Connect();
        public void Disconnect() => _clientSocket.Disconnect();
        public void Send(AFMessageBase msg) => _clientSocket.Send(msg);
        //public void SendEncodeRequest(VideoSourceData data) => _clientSocket.Send()
        #endregion PUBLIC FUNCTIONS

        //protected override void OnTimerElapsed(object obj) => base.OnTimerElapsed(obj);

        #region PRIVATE FUNCTIONS
        private void ProcessMessage(AFMessageBase msg)
        {
            switch (msg.MessageType)
            {
                case AFMessageType.CLIENT_UPDATE:
                {
                    _mainWindow.UpdateEncodingJobs(((ClientUpdateMessage)msg).Data.EncodingJobs);
                    return;
                }
                case AFMessageType.CLIENT_CONNECT:
                {
                    _mainWindow.UpdateVideoSource(((ClientConnectMessage)msg).Data.VideoSourceFiles);
                    _mainWindow.UpdateShowSource(((ClientConnectMessage)msg).Data.ShowSourceFiles);
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
