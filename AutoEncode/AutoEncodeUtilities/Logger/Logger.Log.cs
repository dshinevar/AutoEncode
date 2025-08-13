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

// LOG FUNCTIONS
public partial class Logger : ILogger
{
    #region Public Log Functions
    public void LogInfo(string msg, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "")
        => LogInfo([msg], moduleName, details, callingMemberName);
    public void LogInfo(IEnumerable<string> messages, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "")
        => AddLog(Severity.INFO, messages, moduleName, callingMemberName, details);

    public void LogWarning(string msg, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "")
        => LogWarning([msg], moduleName, details, callingMemberName);
    public void LogWarning(IEnumerable<string> messages, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "")
        => AddLog(Severity.WARNING, messages, moduleName, callingMemberName, details);

    public void LogError(string msg, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "")
        => LogError([msg], moduleName, details, callingMemberName);
    public void LogError(IEnumerable<string> messages, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "")
        => AddLog(Severity.ERROR, messages, moduleName, callingMemberName, details);

    public void LogException(Exception ex, string msg, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "")
        => AddLog(Severity.ERROR, [msg], moduleName, callingMemberName, details, ex);

    public void LogFatal(string msg, string moduleName = "", object details = null, [CallerMemberName] string callingMemberName = "")
        => AddLog(Severity.FATAL, [msg], moduleName, callingMemberName, details);
    #endregion Public Log Functions


    #region Private Log Functions
    private void AddLog(Severity severity, IEnumerable<string> messages, string moduleName = "", string callingMemberName = "", object details = null, Exception exception = null)
        => _logs.TryAdd(new(severity, messages, moduleName, callingMemberName, details, exception));

    private void Log(LogData log)
    {
        List<string> messages = log.Messages.ToList();
        // Exception handling
        if (log.Exception is not null)
        {
            messages.Add($"Exception: {log.Exception.Message}");

            Exception innerEx = log.Exception.InnerException;
            while (innerEx is not null)
            {
                messages.Add(innerEx.Message);
                innerEx = innerEx.InnerException;
            }

            messages.AddRange(log.Exception.StackTrace.Split(Environment.NewLine));
        }

        // Don't waste time if no messages
        if (messages.Count == 0)
            return;

        // Details handling
        if (log.Details is not null)
        {
            messages.Add("Details:");
            PropertyInfo[] detailsProperties = log.Details.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            for (int i = 0; i < detailsProperties.Length; i++)
            {
                messages.AddRange(GenerateDetailsMessages(detailsProperties[i].Name, detailsProperties[i].GetValue(log.Details), 3));
            }
        }

        // Final log handling

        StringBuilder sbLogMsg = new();

        sbLogMsg.Append($"[{DateTime.Now:MM/dd/yyyy HH:mm:ss}] - [{Enum.GetName(typeof(Severity), (int)log.Severity)}]");

        string threadAndCallingMemberName = HelperMethods.JoinFilterWrap(string.Empty, "[", "]", log.ModuleName, log.CallingMemberName);

        if (string.IsNullOrWhiteSpace(threadAndCallingMemberName) is false)
        {
            sbLogMsg.Append(threadAndCallingMemberName);
        }

        sbLogMsg.Append(": ");
        int spacing = sbLogMsg.Length;
        sbLogMsg.AppendLine($"{messages[0]}");

        for (int i = 1; i < messages.Count; i++)
        {
            sbLogMsg.Append(' ', spacing)
                    .AppendLine($"{messages[i].TrimEnd('\r', '\n')}");
        }

        Debug.Write(sbLogMsg.ToString());

        try
        {
            File.AppendAllText(LogFileFullPath, sbLogMsg.ToString());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to log to file ({LogFileFullPath}) : {ex.Message}");
        }

        // Check for rollover after logging
        CheckAndDoRollover();
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
    #endregion Private Log Functions
}
