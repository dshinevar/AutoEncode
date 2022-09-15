using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Logger;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AutomatedFFmpegServer.TaskFactory
{
    public static partial class EncodingJobTaskFactory
    {
        /// <summary>Checks for a cancellation token. Returns true if task was cancelled. </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <param name="job"><see cref="EncodingJob"/> whose status will be reset if cancelled.</param>
        /// <param name="logger"><see cref="Logger"/></param>
        /// <param name="callingFunctionName">Calling method name.</param>
        /// <returns>True if cancelled; False otherwise.</returns>
        public static bool CheckForCancellation(EncodingJob job, Logger logger, CancellationToken cancellationToken, [CallerMemberName] string callingFunctionName = "")
        {
            bool cancel = false;
            if (cancellationToken.IsCancellationRequested)
            {
                // Reset Status
                job.ResetStatus();
                logger.LogInfo($"{callingFunctionName} was cancelled for {job}", callingMemberName: callingFunctionName);
                Debug.WriteLine($"{callingFunctionName} was cancelled for {job}");
                cancel = true;
            }
            return cancel;
        }
    }
}
