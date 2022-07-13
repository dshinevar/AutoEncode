using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;

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
        private string LogFileLocation { get; set; }
        private long MaxSizeInBytes { get; set; }
        private int BackupFileCount { get; set; }
        private object FileLock = new();

        public Logger(string logFileLocation, long maxSizeInBytes = -1, int backupFileCount = 0)
        {
            LogFileLocation = logFileLocation;
            MaxSizeInBytes = maxSizeInBytes;
            BackupFileCount = backupFileCount;
        }

        #region Log Functions
        public void LogDebug(string msg, [CallerMemberName] string callingMemberName = "", string threadName = "") => Log(Severity.DEBUG, msg, callingMemberName, threadName);
        public void LogInfo(string msg, [CallerMemberName] string callingMemberName = "", string threadName = "") => Log(Severity.INFO, msg, callingMemberName, threadName);
        public void LogError(string msg, [CallerMemberName] string callingMemberName = "", string threadName = "") => Log(Severity.ERROR, msg, callingMemberName, threadName);
        public void LogFatal(string msg, [CallerMemberName] string callingMemberName = "", string threadName = "") => Log(Severity.FATAL, msg, callingMemberName, threadName);

        private void Log(Severity severity, string msg, [CallerMemberName] string callingMemberName = "", string threadName = "")
        {
            StringBuilder sbLogMsg = new StringBuilder();

            sbLogMsg.Append($"[{DateTime.Now:MM/dd/yyyy HH:mm:ss}][{Enum.GetName(typeof(Severity), (int)severity)}]");
            if (string.IsNullOrEmpty(threadName))
            {
                if (!string.IsNullOrEmpty(callingMemberName))
                {
                    sbLogMsg.Append($"[{callingMemberName}]: ");
                }
            }
            else
            {
                sbLogMsg.Append($"[{threadName}]");

                if (!string.IsNullOrEmpty(callingMemberName))
                {
                    sbLogMsg.Append($"[{callingMemberName}]: ");
                }
            }

            sbLogMsg.Append($"{msg}{Environment.NewLine}");

            try
            {
                lock (FileLock)
                {
                    File.AppendAllTextAsync(LogFileLocation, sbLogMsg.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log to file ({LogFileLocation}) : {ex.Message}");
            }
        }
        #endregion Log Functions

        #region Rollover Functions
        public void CheckAndDoRollover()
        {
            if (MaxSizeInBytes > -1)
            {
                FileInfo fileInfo = new FileInfo(LogFileLocation);

                if (fileInfo.Length >= MaxSizeInBytes)
                {

                    lock (FileLock)
                    {
                        try
                        {
                            DoRollover();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to do log file rollover: {ex.Message}");
                        }  
                    }
                }
            }
        }

        private void DoRollover()
        {
            if (BackupFileCount > 0)
            {
                for (int i = BackupFileCount; i > 0; i--)
                {
                    string file = $"{LogFileLocation}.{i}";
                    if (File.Exists(file))
                    {
                        if (i == BackupFileCount)
                        {
                            File.Delete(file);
                        }
                        else
                        {
                            File.Move(file, $"{LogFileLocation}.{i + 1}", true);
                        }
                    }
                }

                File.Move(LogFileLocation, $"{LogFileLocation}.1", true);
            }
            else
            {
                File.WriteAllText(LogFileLocation, string.Empty);
            }
        }
        #endregion Rollover Functions


    }
}
