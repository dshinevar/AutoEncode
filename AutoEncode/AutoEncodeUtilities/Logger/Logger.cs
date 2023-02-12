using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AutoEncodeUtilities.Logger
{
    public enum Severity
    {
        [Description("Debug")]
        DEBUG = 0,
        [Description("Info")]
        INFO = 1,
        [Description("Warning")]
        WARNING,
        [Description("Error")]
        ERROR = 3,
        [Description("Fatal")]
        FATAL = 4
    }

    public class Logger
    {
        private readonly string LogFileFullPath;
        private long MaxSizeInBytes { get; set; }
        private int BackupFileCount { get; set; }
        private readonly object FileLock = new();

        public Logger(string logFileLocation, string logFileName, long maxSizeInBytes = -1, int backupFileCount = 0)
        {
            LogFileFullPath = Path.Combine(logFileLocation, logFileName);
            MaxSizeInBytes = maxSizeInBytes;
            BackupFileCount = backupFileCount;
        }

        #region Log Functions
        /// <summary>Log a Debug message; Only available in Debug builds.</summary>
        /// <param name="msg">Message to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        [Conditional("DEBUG")]
        public void LogDebug(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.DEBUG, new string[] { msg }, threadName, callingMemberName);
        /// <summary>Log a list of debug messages; Only available in Debug builds</summary>
        /// <param name="messages">Messages to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        [Conditional("DEBUG")]
        public void LogDebug(IList<string> messages, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.DEBUG, messages, threadName, callingMemberName);
        /// <summary> Log an Info Message </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        public string LogInfo(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.INFO, new string[] { msg }, threadName, callingMemberName);
        /// <summary>Log a list of info messages.</summary>
        /// <param name="messages">Messages to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        public string LogInfo(IList<string> messages, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.INFO, messages, threadName, callingMemberName);
        /// <summary> Log a Warning Message </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        public string LogWarning(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.WARNING, new string[] { msg }, threadName, callingMemberName);
        /// <summary>Log a list of warning messages.</summary>
        /// <param name="messages">Messages to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        public string LogWarning(IList<string> messages, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.WARNING, messages, threadName, callingMemberName);
        /// <summary> Log an Error Message </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        public string LogError(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.ERROR, new string[] { msg }, threadName, callingMemberName);
        /// <summary>Log a list of Error messages.</summary>
        /// <param name="messages">Messages to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        public string LogError(IList<string> messages, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.ERROR, messages, threadName, callingMemberName);
        /// <summary> Log a Fatal Message </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        public string LogFatal(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.FATAL, new string[] { msg }, threadName, callingMemberName);
        /// <summary> Log an <see cref="Exception"/>. Will log the message, Exception message, and stack trace. </summary>
        /// <param name="msg">Message to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function.</param>
        public string LogException(Exception ex, string msg, string threadName = "", [CallerMemberName] string callingMemberName = "")
        {
            List<string> messages = new() { msg, $"Exception: {ex.Message}" };
            messages.AddRange(ex.StackTrace.Split(Environment.NewLine));
            return LogError(messages, threadName, callingMemberName);
        }


        /// <summary>Base log function; Returns first string from list for usage elsewhere if needed. </summary>
        /// <param name="severity"><see cref="Severity"/></param>
        /// <param name="messages">List of messages to log</param>
        /// <param name="threadName">Thread calling log</param>
        /// <param name="callingMemberName">Calling function</param>
        /// <returns></returns>
        private string Log(Severity severity, IList<string> messages, string threadName = "", string callingMemberName = "")
        {
            if (messages.Any() is false) return string.Empty;

            StringBuilder sbLogMsg = new();

            sbLogMsg.Append($"[{DateTime.Now:MM/dd/yyyy HH:mm:ss}] - [{Enum.GetName(typeof(Severity), (int)severity)}]");

            string threadAndCallingMemberName = HelperMethods.JoinFilterWrap(string.Empty, "[", "]", threadName, callingMemberName);

            if (string.IsNullOrEmpty(threadAndCallingMemberName) is false)
            {
                sbLogMsg.Append(threadAndCallingMemberName);
            }

            sbLogMsg.Append(": ");
            int spacing = sbLogMsg.Length;
            sbLogMsg.AppendLine($"{messages[0]}");

            for (int i = 1; i < messages.Count; i++)
            {
                sbLogMsg.Append(' ', spacing).AppendLine($"{messages[i]}");
            }

            Debug.Write(sbLogMsg.ToString());

            try
            {
                lock (FileLock)
                {
                    File.AppendAllText(LogFileFullPath, sbLogMsg.ToString());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to log to file ({LogFileFullPath}) : {ex.Message}");
            }

            return messages.FirstOrDefault() ?? string.Empty;
        }
        #endregion Log Functions

        #region Rollover Functions
        public bool CheckAndDoRollover()
        {
            bool bSuccess = true;
            try
            {
                if (MaxSizeInBytes > -1)
                {
                    FileInfo fileInfo = new(LogFileFullPath);

                    if (fileInfo.Exists && fileInfo.Length >= MaxSizeInBytes)
                    {
                        lock (FileLock)
                        {
                            DoRollover();
                        }
                    }
                }
            }
            catch (FileNotFoundException fnfEx)
            {
                Debug.WriteLine($"Failed to do log file rollover: {fnfEx.Message}");
                bSuccess = false;
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Debug.WriteLine($"Failed to do log file rollover: {uaEx.Message}");
                bSuccess = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to do log file rollover: {ex.Message}");
                LogException(ex, "Failed to do log file rollover", "Logger");
                bSuccess = false;
            }

            return bSuccess;
        }

        private void DoRollover()
        {
            if (BackupFileCount > 0)
            {
                for (int i = BackupFileCount; i > 0; i--)
                {
                    string file = $"{LogFileFullPath}.{i}";
                    if (File.Exists(file))
                    {
                        if (i == BackupFileCount)
                        {
                            File.Delete(file);
                        }
                        else
                        {
                            File.Move(file, $"{LogFileFullPath}.{i + 1}", true);
                        }
                    }
                }

                File.Move(LogFileFullPath, $"{LogFileFullPath}.1", true);
            }
            else
            {
                File.WriteAllText(LogFileFullPath, string.Empty);
            }
        }
        #endregion Rollover Functions
    }
}
