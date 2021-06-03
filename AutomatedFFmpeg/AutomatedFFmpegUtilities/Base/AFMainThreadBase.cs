using System;
using System.Collections.Generic;
using System.Threading;

namespace AutomatedFFmpegUtilities.Base
{
    /// <summary> Base Class that includes a Timer and Task Queue. </summary>
    public abstract class AFMainThreadBase
    {
        private int _waitTime { get; set; }
        private Timer _timer { get; set; }
        private Queue<Action> _taskQueue { get; set; }
        private ManualResetEvent _timerDispose { get; set; } = new ManualResetEvent(false);
        /// <summary> Constructor; Creates task queue. </summary>
        public AFMainThreadBase(int timerWait = 250)
        {
            _waitTime = timerWait;
            _taskQueue = new Queue<Action>();
        }

        /// <summary>Creates/Starts timer.</summary>
        public virtual void Start() => _timer = new Timer(OnTimerElapsed, _taskQueue, 1000, _waitTime);

        /// <summary> Shuts down main thread. </summary>
        public virtual void Shutdown()
        {
            _taskQueue.Clear();
            _timer.Dispose(_timerDispose);
            _timerDispose.WaitOne();
            _timerDispose.Dispose();
        }

        /// <summary> Base Thread Loop; Checks, dequeues, and invokes tasks. </summary>
        /// <param name="obj">Task Queue</param>
        protected virtual void OnTimerElapsed(object obj)
        {
            Queue<Action> tasks = (Queue<Action>)obj;
            Action task;
            tasks.TryDequeue(out task);
            task?.Invoke();
        }

        /// <summary>Adds task to task queue.</summary>
        /// <param name="task">Action</param>
        protected void AddTask(Action task) => _taskQueue.Enqueue(task);
    }
}
