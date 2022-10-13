using AutomatedFFmpegServer.ServerSocket;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Messages;
using System;

namespace AutomatedFFmpegServer
{
    public partial class AFServerMainThread
    {
        /// <summary> Process Timer: Checks, dequeues, and invokes tasks. </summary>
        /// <param name="obj"></param>
        private void OnProcessTimerElapsed(object obj)
        {
            Action task = null;
            try
            {
                TaskQueue.TryDequeue(out task);
                task?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Error processing a task in the task queue: {task?.Method?.Name ?? "NULL TASK"}");
            }

            if (ServerSocket?.IsConnected() ?? false) SendMessage(ServerToClientMessageFactory.CreateClientUpdateMessage(new ClientUpdateData()));

            Logger?.CheckAndDoRollover();
        }

        #region Add Task Functions
        /// <summary>Adds task to task queue.</summary>
        /// <param name="task">Action</param>
        private void AddTask(Action task) => TaskQueue.Enqueue(task);
        /// <summary>Adds ProcessMessage task to Task Queue (Client to Server Message).</summary>
        /// <param name="msg">AFMessageBase</param>
        public void AddProcessMessage(AFMessageBase msg) => AddTask(() => ProcessMessage(msg));
        public void AddSendClientConnectData() => AddTask(() => SendClientConnectData());
        /// <summary>Adds SendMessage task to Task Queue (Server To Client Message). </summary>
        /// <param name="msg">AFMessageBase</param>
        public void AddSendMessage(AFMessageBase msg) => AddTask(() => SendMessage(msg));
        #endregion Add Task Functions

        #region SendMessage Functions
        /// <summary>Send message to client.</summary>
        /// <param name="msg"></param>
        private void SendMessage(AFMessageBase msg) => ServerSocket.Send(msg);

        private void SendClientConnectData()
        {
            ClientConnectData clientConnect = new ClientConnectData()
            {
                VideoSourceFiles = EncodingJobFinderThread.GetMovieSourceFiles(),
                ShowSourceFiles = EncodingJobFinderThread.GetShowSourceFiles()
            };
            SendMessage(ServerToClientMessageFactory.CreateClientConnectMessage(clientConnect));
        }
        #endregion SendMessage Functions

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
    }
}
