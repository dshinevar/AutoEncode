using AutomatedFFmpegServer.ServerSocket;
using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Messages;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegServer.WorkerThreads;
using AutomatedFFmpegUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AutomatedFFmpegServer
{
    public partial class AFServerMainThread
    {
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
        #endregion Add Functions

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

        #region Process Functions
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
                            => EncodingJobTasks.BuildEncodingJob(jobToBuild, Config.ServerSettings.FFmpegDirectory, Logger, EncodingJobBuilderCancellationToken.Token), EncodingJobBuilderCancellationToken.Token);
                    }
                }

                // Check if task is done (or null -- first time setup)
                if (EncodingTask?.IsCompletedSuccessfully ?? true)
                {
                    EncodingJob jobToEncode = EncodingJobQueue.GetNextEncodingJobWithStatus(EncodingJobStatus.ANALYZED);
                    if (jobToEncode is not null)
                    {
                        EncodingTask = Task.Factory.StartNew(()
                            => EncodingJobTasks.Encode(jobToEncode, Config.ServerSettings.FFmpegDirectory, Logger, EncodingCancellationToken.Token), EncodingJobBuilderCancellationToken.Token);
                    }
                }
            }

            if (ServerSocket?.IsConnected() ?? false) SendMessage(ServerToClientMessageFactory.CreateClientUpdateMessage(new ClientUpdateData()));

            //(Logger?.CheckAndDoRollover();
        }

        /// <summary> Task Timer: Checks, dequeues, and invokes tasks. </summary>
        /// <param name="obj">Task Queue</param>
        private void OnTaskTimerElapsed(object obj)
        {
            Action task;
            TaskQueue.TryDequeue(out task);
            task?.Invoke();
        }
        #endregion Process Functions
    }
}
