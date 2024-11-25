using AutoEncodeUtilities.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace AutoEncodeUtilities.Logger;

public class Logger : ILogger
{
    private readonly object _fileLock = new();
    private bool _initialized = false;

    public string LogFileDirectory { get; set; }
    public string LogFileFullPath { get; set; }
    public long MaxSizeInBytes { get; set; }
    public int BackupFileCount { get; set; }

    public Logger() { }

    public bool Initialize(string logFileDirectory, string logFileName, long maxSizeInBytes = -1, int backupFileCount = 0)
    {
        try
        {
            if (_initialized is false)
            {
                LogFileDirectory = logFileDirectory;
                LogFileFullPath = Path.Combine(LogFileDirectory, logFileName);
                MaxSizeInBytes = maxSizeInBytes;
                BackupFileCount = backupFileCount;

                DirectoryInfo directoryInfo = Directory.CreateDirectory(LogFileDirectory);
                if (directoryInfo is not null)
                {
                    _initialized = true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create / find log directory: {logFileDirectory}{Environment.NewLine}{ex.Message}");
        }

        return _initialized;
    }

    #region Log Functions
    public string LogInfo(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "") => LogInfo([msg], threadName, callingMemberName);
    public string LogInfo(IList<string> messages, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.INFO, messages, threadName, callingMemberName);

    public string LogWarning(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "") => LogWarning([msg], threadName, callingMemberName);
    public string LogWarning(IList<string> messages, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.WARNING, messages, threadName, callingMemberName);

    public string LogError(string msg, string threadName = "", object details = null, [CallerMemberName] string callingMemberName = "") => LogError([msg], threadName, details, callingMemberName);
    public string LogError(IList<string> messages, string threadName = "", object details = null, [CallerMemberName] string callingMemberName = "") => Log(Severity.ERROR, messages, threadName, details, callingMemberName);

    public string LogFatal(string msg, string threadName = "", [CallerMemberName] string callingMemberName = "") => Log(Severity.FATAL, [msg], threadName, callingMemberName);

    public string LogException(Exception ex, string msg, string threadName = "", object details = null, [CallerMemberName] string callingMemberName = "")
    {
        List<string> messages = [msg];
        messages.Add($"Exception: {ex.Message}");

        Exception innerEx = ex.InnerException;
        while (innerEx is not null)
        {
            messages.Add(innerEx.Message);
            innerEx = innerEx.InnerException;
        }

        messages.AddRange(ex.StackTrace.Split(Environment.NewLine));
        return Log(Severity.ERROR, messages, threadName, details, callingMemberName);
    }

    /// <summary>Base log method that handles additional details; Returns first string from list for usage elsewhere if needed. </summary>
    /// <param name="severity"><see cref="Severity"/></param>
    /// <param name="messages">List of messages to log</param>
    /// <param name="threadName">Thread calling log</param>
    /// <param name="details">Additional details to log</param>
    /// <param name="callingMemberName">Calling function</param>
    /// <returns>Returns first string from list for usage elsewhere if needed.</returns>
    private string Log(Severity severity, IList<string> messages, string threadName = "", object details = null, string callingMemberName = "")
    {
        if (details is not null)
        {
            messages.Add("Details:");
            PropertyInfo[] detailsProperties = details.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            for (int i = 0; i < detailsProperties.Length; i++)
            {
                messages.AddRange(GenerateDetailsMessages(detailsProperties[i].Name, detailsProperties[i].GetValue(details), 3));
            }
        }

        return Log(severity, messages, threadName, callingMemberName);
    }

    private static List<string> GenerateDetailsMessages(string name, object details, int padding = 0)
    {
        List<string> detailsMessages = [];

        StringBuilder sbDetailMessage = new();
        Type type = details?.GetType();
        if ((type is null) || (details is null))
        {
            sbDetailMessage.Append(' ', padding).Append($"{name} = NULL");
            detailsMessages.Add(sbDetailMessage.ToString());
        }
        else if (type.IsPrimitive || (type == typeof(string)) || (type == typeof(TimeSpan)) || (type == typeof(DateTime)))
        {
            sbDetailMessage.Append(' ', padding).Append($"{name} = {details}");
            detailsMessages.Add(sbDetailMessage.ToString());
        }
        else
        {
            if (details is IEnumerable enumerable)
            {
                Type childType = type.GetGenericArguments()[0];
                sbDetailMessage.Append(' ', padding).Append($"{name} (IEnumerable<{childType}>) = ");
                detailsMessages.Add(sbDetailMessage.ToString());

                int index = 0;
                foreach (var item in enumerable)
                {
                    detailsMessages.AddRange(GenerateDetailsMessages($"[{index}]", item, padding + 3));
                    index++;
                }
            }
            else if (type.IsClass)
            {
                sbDetailMessage.Append(' ', padding).Append($"{name} = ");
                detailsMessages.Add(sbDetailMessage.ToString());
                PropertyInfo[] detailsProperties = details.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                if (detailsProperties.Length > 0)
                {
                    for (int i = 0; i < detailsProperties.Length; i++)
                    {
                        detailsMessages.AddRange(GenerateDetailsMessages(detailsProperties[i].Name, detailsProperties[i].GetValue(details), padding + 3));
                    }
                }
            }
        }

        return detailsMessages;
    }

    /// <summary>Base log method; Returns first string from list for usage elsewhere if needed. </summary>
    /// <param name="severity"><see cref="Severity"/></param>
    /// <param name="messages">List of messages to log</param>
    /// <param name="threadName">Thread calling log</param>
    /// <param name="callingMemberName">Calling function</param>
    /// <returns>Returns first string from list for usage elsewhere if needed.</returns>
    private string Log(Severity severity, IList<string> messages, string threadName = "", string callingMemberName = "")
    {
        if (_initialized is false) throw new Exception("Logger not initialized");

        if (messages.Any() is false) return string.Empty;

        StringBuilder sbLogMsg = new();

        sbLogMsg.Append($"[{DateTime.Now:MM/dd/yyyy HH:mm:ss}] - [{Enum.GetName(typeof(Severity), (int)severity)}]");

        string threadAndCallingMemberName = HelperMethods.JoinFilterWrap(string.Empty, "[", "]", threadName, callingMemberName);

        if (string.IsNullOrWhiteSpace(threadAndCallingMemberName) is false)
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
            lock (_fileLock)
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
        if (_initialized is false) throw new Exception("Logger not initialized");

        bool bSuccess = true;
        try
        {
            if (MaxSizeInBytes > -1)
            {
                FileInfo fileInfo = new(LogFileFullPath);

                if (fileInfo.Exists && fileInfo.Length >= MaxSizeInBytes)
                {
                    lock (_fileLock)
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
            LogException(ex, "Failed to do log file rollover", "Logger", new { LogFileFullPath, MaxSizeInBytes });
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
