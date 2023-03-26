using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Messages;
using System;
using System.Collections.Generic;

namespace AutoEncodeServer
{
    public partial class AEServerMainThread
    {
        private string ProcessThreadName => $"{ThreadName}-Process";
        private Queue<Action> TaskQueue { get; set; } = new Queue<Action>();

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

            Logger?.CheckAndDoRollover();
        }

        #region Add Task Functions
        /// <summary>Adds task to task queue.</summary>
        /// <param name="task">Action</param>
        private void AddTask(Action task) => TaskQueue.Enqueue(task);
        /// <summary>Adds ProcessMessage task to Task Queue (Client to Server Message).</summary>
        /// <param name="msg">AEMessageBase</param>
        public void AddProcessMessage(AEMessage msg) => AddTask(() => ProcessMessage(msg));
        /// <summary>Adds SendMessage task to Task Queue (Server To Client Message). </summary>
        /// <param name="msg">AEMessageBase</param>
        #endregion Add Task Functions

        #region SendMessage Functions
        #endregion SendMessage Functions

        /// <summary>Process received message from client. </summary>
        /// <param name="msg"></param>
        private void ProcessMessage(AEMessage msg)
        {

        }
    }
}
