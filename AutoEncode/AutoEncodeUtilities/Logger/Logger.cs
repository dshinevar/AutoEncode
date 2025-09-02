using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeUtilities.Logger;

public partial class Logger : ILogger
{
    private readonly record struct LogData(
        Severity Severity,
        IEnumerable<string> Messages,
        string ModuleName,
        string CallingMemberName,
        object Details,
        Exception Exception);

    private bool _initialized = false;
    private readonly BlockingCollection<LogData> _logs = [];
    private readonly CancellationTokenSource _shutdownCancellationTokenSource = new();

    public string LogFileDirectory { get; set; }
    public string LogFileFullPath { get; set; }
    public long MaxSizeInBytes { get; set; }
    public int BackupFileCount { get; set; }

    #region Init / Run / Stop

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
                    // Initialize fails if rollover fails
                    _initialized = CheckAndDoRollover();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create / find log directory: {logFileDirectory}{Environment.NewLine}{ex.Message}");
        }

        return _initialized;
    }

    public Task Run()
    {
        if (_initialized is false)
            throw new InvalidOperationException("Logger is not initialized.");

        return Task.Run(() =>
        {
            const int MaxLogFails = 5;
            int logFailCount = 0;
            try
            {
                foreach (LogData log in _logs.GetConsumingEnumerable(_shutdownCancellationTokenSource.Token))
                {
                    try
                    {
                        Log(log);
                    }
                    catch (Exception e)
                    {
                        if (logFailCount < MaxLogFails)
                        {
                            // Attempt to add a log to figure out what broke
                            LogException(e, "Error occurred while logging.", nameof(Logger), new { log });
                            logFailCount++;
                        }
                        else
                        {
                            // After a few failures, something is really wrong
                            // What to do?
                        }

                    } 
                }
            }
            catch (OperationCanceledException) { }

        }, _shutdownCancellationTokenSource.Token);
    }    

    public void Stop(bool kill = false)
    {
        _logs.CompleteAdding();

        // If kill, just shutdown logger, don't worry about messages
        if (kill is true)
        {
            _shutdownCancellationTokenSource.Cancel();
        }
        // Otherwise try to let logger finish
        else
        {
            while (_logs.Count != 0) ;
            _shutdownCancellationTokenSource.Cancel();
        }
    }
    #endregion Init / Run / Stop

    #region Rollover Functions
    private bool CheckAndDoRollover()
    {
        bool success = true;
        try
        {
            if (MaxSizeInBytes > -1)
            {
                FileInfo fileInfo = new(LogFileFullPath);

                if (fileInfo.Exists && fileInfo.Length >= MaxSizeInBytes)
                {
                    DoRollover();
                }
            }
        }
        catch (FileNotFoundException fnfEx)
        {
            Debug.WriteLine($"Failed to do log file rollover: {fnfEx.Message}");
            success = false;
        }
        catch (UnauthorizedAccessException uaEx)
        {
            Debug.WriteLine($"Failed to do log file rollover: {uaEx.Message}");
            success = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to do log file rollover: {ex.Message}");
            LogException(ex, "Failed to do log file rollover", "Logger", details: new { LogFileFullPath, MaxSizeInBytes });
            success = false;
        }

        return success;
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
