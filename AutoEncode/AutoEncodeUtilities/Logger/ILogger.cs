using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AutoEncodeUtilities.Logger
{
    public interface ILogger
    {
        /// <summary> Log an Info Message </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        string LogInfo(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "");
        /// <summary>Log a list of info messages.</summary>
        /// <param name="messages">Messages to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        string LogInfo(IList<string> messages, string threadName = "", [CallerMemberName] string callingMemberName = "");

        /// <summary> Log a Warning Message </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        string LogWarning(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "");
        /// <summary>Log a list of warning messages.</summary>
        /// <param name="messages">Messages to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        string LogWarning(IList<string> messages, string threadName = "", [CallerMemberName] string callingMemberName = "");

        /// <summary> Log an Error Message </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        string LogError(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "");
        /// <summary>Log a list of Error messages.</summary>
        /// <param name="messages">Messages to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        string LogError(IList<string> messages, string threadName = "", [CallerMemberName] string callingMemberName = "");

        /// <summary> Log a Fatal Message </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        string LogFatal(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "");

        /// <summary> Log an <see cref="Exception"/>. Will log the message, Exception message, and stack trace. </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        string LogException(Exception ex, string msg, string threadName = "", [CallerMemberName] string callingMemberName = "");

        /// <summary>Checks to see if Log file is ready for rollover and does it if so.</summary>
        /// <returns>True if success</returns>
        bool CheckAndDoRollover();
    }
}
