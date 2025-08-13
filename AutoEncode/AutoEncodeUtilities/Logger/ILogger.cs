using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AutoEncodeUtilities.Logger;

public interface ILogger
{
    /// <summary>Full path of the log file location </summary>
    string LogFileFullPath { get; }

    /// <summary>Maximum size a log file should be in bytes (for rollover to occur)</summary>
    long MaxSizeInBytes { get; }

    /// <summary>Number of previous log files to keep around</summary>
    int BackupFileCount { get; }

    /// <summary>Sets initial data for use.</summary>
    /// <param name="logFileDirectory">Directory to place log file in</param>
    /// <param name="logFileName">Name of log file</param>
    /// <param name="maxSizeInBytes">Max size in bytes file should be before rolling over (when calling CheckAndDoRollover)</param>
    /// <param name="backupFileCount">Number of backup log files to keep</param>
    /// <returns>True if initialization succeeds; False, otherwise.</returns>
    bool Initialize(string logFileDirectory, string logFileName, long maxSizeInBytes = -1, int backupFileCount = 0);

    Task Run();

    void Stop(bool kill = false);

    /// <summary> Log an Info Message </summary>
    /// <param name="msg">Message to log</param>
    /// <param name="moduleName">Module calling log</param>
    /// <param name="callingMemberName">Calling function.</param>
    /// <param name="details">Additional details to log</param>
    void LogInfo(string msg, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "");
    /// <summary>Log a list of info messages.</summary>
    /// <param name="messages">Messages to log</param>
    /// <param name="moduleName">Module calling log</param>
    /// <param name="callingMemberName">Calling function.</param>
    /// <param name="details">Additional details to log</param>
    void LogInfo(IEnumerable<string> messages, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "");

    /// <summary> Log a Warning Message </summary>
    /// <param name="msg">Message to log</param>
    /// <param name="moduleName">Module calling log</param>
    /// <param name="callingMemberName">Calling function.</param>
    /// <param name="details">Additional details to log</param>
    void LogWarning(string msg, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "");
    /// <summary>Log a list of warning messages.</summary>
    /// <param name="messages">Messages to log</param>
    /// <param name="moduleName">Module calling log</param>
    /// <param name="callingMemberName">Calling function.</param>
    /// <param name="details">Additional details to log</param>
    void LogWarning(IEnumerable<string> messages, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "");

    /// <summary> Log an Error Message </summary>
    /// <param name="msg">Message to log</param>
    /// <param name="moduleName">Module calling log</param>
    /// <param name="callingMemberName">Calling function.</param>
    /// <param name="details">Additional details to log</param>
    void LogError(string msg, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "");
    /// <summary>Log a list of Error messages.</summary>
    /// <param name="messages">Messages to log</param>
    /// <param name="moduleName">Module calling log</param>
    /// <param name="callingMemberName">Calling function.</param>
    /// <param name="details">Additional details to be logged</param>
    void LogError(IEnumerable<string> messages, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "");

    /// <summary> Log a Fatal Message </summary>
    /// <param name="msg">Message to log</param>
    /// <param name="moduleName">Module calling log</param>
    /// <param name="callingMemberName">Calling function.</param>
    /// <param name="details">Additional details to be logged</param>
    void LogFatal(string msg, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "");

    /// <summary> Log an <see cref="Exception"/>. Will log the message, Exception message, and stack trace. </summary>
    /// <param name="msg">Message to log</param>
    /// <param name="moduleName">Module calling log</param>
    /// <param name="callingMemberName">Calling function.</param>
    /// <param name="details">Additional details to be logged</param>
    void LogException(Exception ex, string msg, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "");
}
