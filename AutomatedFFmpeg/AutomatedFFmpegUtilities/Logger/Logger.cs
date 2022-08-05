using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace AutomatedFFmpegUtilities.Logger
{
    public enum Severity
    {
        [Description("Debug")]
        DEBUG = 0,
        [Description("Info")]
        INFO = 1,
        [Description("Error")]
        ERROR = 2,
        [Description("Fatal")]
        FATAL = 3
    }

    public class Logger
    {
        private readonly string LogFileFullPath;
        private long MaxSizeInBytes { get; set; }
        private int BackupFileCount { get; set; }
        private readonly object FileLock = new();

        public Logger(string logFileLocation, string logFileName, long maxSizeInBytes = -1, int backupFileCount = 0)
        {
            LogFileFullPath = $@"{logFileLocation.RemoveEndingSlashes()}{Path.DirectorySeparatorChar}{logFileName}";
            MaxSizeInBytes = maxSizeInBytes;
            BackupFileCount = backupFileCount;
        }

        #region Log Functions
        [Conditional("DEBUG")]
        public void LogDebug(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.DEBUG, msg, threadName, callingMemberName);
        public void LogInfo(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.INFO, msg, threadName, callingMemberName);
        public void LogInfo(IList<string> messages, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.INFO, messages, threadName, callingMemberName);
        public void LogError(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.ERROR, msg, threadName, callingMemberName);
        public void LogError(IList<string> messages, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.ERROR, messages, threadName, callingMemberName);
        public void LogFatal(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.FATAL, msg, threadName, callingMemberName);
        public void LogException(Exception ex, string msg, string threadName = "", [CallerMemberName] string callingMemberName = "")
            => LogError($"{msg} (Exception: {ex.Message})", threadName, callingMemberName);

        private void Log(Severity severity, string msg, string threadName = "", string callingMemberName = "")
        {
            StringBuilder sbLogMsg = new();

            sbLogMsg.Append($"[{DateTime.Now:MM/dd/yyyy HH:mm:ss}] - [{Enum.GetName(typeof(Severity), (int)severity)}]");
            if (string.IsNullOrEmpty(threadName))
            {
                if (!string.IsNullOrEmpty(callingMemberName))
                {
                    sbLogMsg.Append($"[{callingMemberName}]");
                }
            }
            else
            {
                sbLogMsg.Append($"[{threadName}]");

                if (!string.IsNullOrEmpty(callingMemberName))
                {
                    sbLogMsg.Append($"[{callingMemberName}]");
                }
            }

            sbLogMsg.Append($": {msg}{Environment.NewLine}");

            try
            {
                lock (FileLock)
                {
                    File.AppendAllTextAsync(LogFileFullPath, sbLogMsg.ToString());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to log to file ({LogFileFullPath}) : {ex.Message}");
            }
        }

        private void Log(Severity severity, IList<string> messages, string threadName = "", string callingMemberName = "")
        {
            StringBuilder sbLogMsg = new();

            sbLogMsg.Append($"[{DateTime.Now:MM/dd/yyyy HH:mm:ss}] - [{Enum.GetName(typeof(Severity), (int)severity)}]");
            if (string.IsNullOrEmpty(threadName))
            {
                if (!string.IsNullOrEmpty(callingMemberName))
                {
                    sbLogMsg.Append($"[{callingMemberName}]");
                }
            }
            else
            {
                sbLogMsg.Append($"[{threadName}]");

                if (!string.IsNullOrEmpty(callingMemberName))
                {
                    sbLogMsg.Append($"[{callingMemberName}]");
                }
            }

            sbLogMsg.Append(": ");
            int spacing = sbLogMsg.Length;
            sbLogMsg.Append($"{messages[0]}{Environment.NewLine}");

            for (int i = 1; i < messages.Count; i++)
            {
                sbLogMsg.Append(' ', spacing).Append($"{messages[i]}{Environment.NewLine}");
            }

            try
            {
                lock (FileLock)
                {
                    File.AppendAllTextAsync(LogFileFullPath, sbLogMsg.ToString());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to log to file ({LogFileFullPath}) : {ex.Message}");
            }
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
                    FileInfo fileInfo = new FileInfo(LogFileFullPath);

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
