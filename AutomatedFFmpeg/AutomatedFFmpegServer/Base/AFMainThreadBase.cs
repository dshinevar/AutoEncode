using System;
using System.Collections.Generic;
using System.Threading;

namespace AutomatedFFmpegServer.Base
{
    /// <summary> Base Class that includes a Timer and Task Queue. </summary>
    public abstract class AFMainThreadBase
    {
        private int TimerWaitTime { get; set; }
        private Timer TaskTimer { get; set; }
        private Queue<Action> TaskQueue { get; set; }
        private ManualResetEvent TimerDispose { get; set; } = new ManualResetEvent(false);
        /// <summary> Constructor; Creates task queue. </summary>
        public AFMainThreadBase(int timerWait = 250)
        {
            TimerWaitTime = timerWait;
            TaskQueue = new Queue<Action>();
        }

        /// <summary>Creates/Starts timer.</summary>
        public virtual void Start() => TaskTimer = new Timer(OnTaskTimerElapsed, TaskQueue, 1000, TimerWaitTime);

        /// <summary> Shuts down main thread. </summary>
        public virtual void Shutdown()
        {
            TaskQueue.Clear();
            TaskTimer.Dispose(TimerDispose);
            TimerDispose.WaitOne();
            TimerDispose.Dispose();
        }

        /// <summary> Task Timer: Checks, dequeues, and invokes tasks. </summary>
        /// <param name="obj">Task Queue</param>
        private void OnTaskTimerElapsed(object obj)
        {
            Queue<Action> tasks = (Queue<Action>)obj;
            Action task;
            tasks.TryDequeue(out task);
            task?.Invoke();
        }

        /// <summary>Adds task to task queue.</summary>
        /// <param name="task">Action</param>
        protected void AddTask(Action task) => TaskQueue.Enqueue(task);
    }
}
