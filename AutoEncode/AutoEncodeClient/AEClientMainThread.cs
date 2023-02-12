using AutoEncodeClient.ClientSocket;
using AutoEncodeUtilities.Messages;
using AutoEncodeClient.Config;
using System;
using AutoEncodeUtilities.Enums;
using System.Collections.Generic;
using AutoEncodeUtilities.Data;

namespace AutoEncodeClient
{
    public class AEClientMainThread
    {
        //private MainWindow _mainWindow { get; set; }
        private AEClientSocket _clientSocket { get; set; }
        private AEClientConfig _clientConfig { get; set; }

        /// <summary> Constructor </summary>
        /// <param name="wnd">MainWindow handle</param>
        /// <param name="config">Client config</param>
        public AEClientMainThread(AEClientConfig config)
        {
            //_mainWindow = wnd;
            _clientConfig = config;
        }

        #region PUBLIC FUNCTIONS
        /// <summary> Starts AEClientMainThread; Client socket tries to connect. </summary>
        public  void Start()
        {
            _clientSocket = new AEClientSocket(this, _clientConfig.ServerIP, _clientConfig.Port);
            _clientSocket.Connect();
        }
        /// <summary>Shuts down AEClientMainThread; Closes socket. </summary>
        public void Shutdown()
        {
            _clientSocket.Close();
        }
        //public void AddProcessMessage(AEMessageBase msg) => AddTask(new Action(() => ProcessMessage(msg)));
        public void Connect() => _clientSocket.Connect();
        public void Disconnect() => _clientSocket.Disconnect();
        public void Send(AEMessageBase msg) => _clientSocket.Send(msg);
        //public void SendEncodeRequest(VideoSourceData data) => _clientSocket.Send()
        #endregion PUBLIC FUNCTIONS

        //protected override void OnTimerElapsed(object obj) => base.OnTimerElapsed(obj);

        #region PRIVATE FUNCTIONS
        private void ProcessMessage(AEMessageBase msg)
        {
            switch (msg.MessageType)
            {
                case AEMessageType.CLIENT_UPDATE:
                {
                    //_mainWindow.UpdateEncodingJobs(((ClientUpdateMessage)msg).Data.EncodingJobs);
                    return;
                }
                case AEMessageType.CLIENT_CONNECT:
                {
                    //_mainWindow.UpdateVideoSource(((ClientConnectMessage)msg).Data.VideoSourceFiles);
                    //_mainWindow.UpdateShowSource(((ClientConnectMessage)msg).Data.ShowSourceFiles);
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
