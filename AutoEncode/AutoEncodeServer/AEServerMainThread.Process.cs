using AutoEncodeServer.ServerSocket;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Messages;
using System;

namespace AutoEncodeServer
{
    public partial class AEServerMainThread
    {
        private static string ProcessThreadName => $"{ThreadName}-Process";

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
                Logger.LogException(ex, $"Error processing a task in the task queue: {task?.Method?.Name ?? "NULL TASK"}", ProcessThreadName);
            }

            if (ServerSocket?.IsConnected() ?? false) SendMessage(ServerToClientMessageFactory.CreateClientUpdateMessage(new ClientUpdateData()));

            Logger?.CheckAndDoRollover();
        }

        #region Add Task Functions
        /// <summary>Adds task to task queue.</summary>
        /// <param name="task">Action</param>
        private void AddTask(Action task) => TaskQueue.Enqueue(task);
        /// <summary>Adds ProcessMessage task to Task Queue (Client to Server Message).</summary>
        /// <param name="msg">AEMessageBase</param>
        public void AddProcessMessage(AEMessageBase msg) => AddTask(() => ProcessMessage(msg));
        public void AddSendClientConnectData() => AddTask(() => SendClientConnectData());
        /// <summary>Adds SendMessage task to Task Queue (Server To Client Message). </summary>
        /// <param name="msg">AEMessageBase</param>
        public void AddSendMessage(AEMessageBase msg) => AddTask(() => SendMessage(msg));
        #endregion Add Task Functions

        #region SendMessage Functions
        /// <summary>Send message to client.</summary>
        /// <param name="msg"></param>
        private void SendMessage(AEMessageBase msg) => ServerSocket.Send(msg);

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
        private void ProcessMessage(AEMessageBase msg)
        {
            switch (msg.MessageType)
            {
                case AEMessageType.CLIENT_REQUEST:
                {
                    SendClientConnectData();
                    break;
                }
            }
        }
    }
}
