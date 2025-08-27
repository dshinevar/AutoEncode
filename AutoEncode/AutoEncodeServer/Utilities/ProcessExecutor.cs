using AutoEncodeServer.Utilities.Data;
using AutoEncodeServer.Utilities.Interfaces;
using AutoEncodeUtilities.Logger;
using AutoEncodeUtilities.Process;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.Utilities;

public class ProcessExecutor : IProcessExecutor
{
    public ILogger Logger { get; set; }

    public ProcessResult<string> Execute(ProcessExecutionData processExecutionData, CancellationToken cancellationToken = default)
    {
        Process process = null;
        int? exitCode = null;
        bool processStarted = false;
        List<string> processErrorLogs = [];
        StringBuilder sbOutput = new();

        CancellationTokenRegistration cancellationTokenRegistration;
        // Register on cancellation to kill the process
        if (cancellationToken != CancellationToken.None)
        {
            cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                if (processStarted is true)
                    process?.Kill(true);
            });
        }
        else
        {
            cancellationTokenRegistration = new();
        }

        try
        {
            ProcessStartInfo processStartInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = processExecutionData.ReturnStandardOutput & !processExecutionData.ReturnStandardError,
                RedirectStandardError = true,
                FileName = processExecutionData.FileName,
                Arguments = processExecutionData.Arguments,
            };

            Func<string, bool> outputCheck = processExecutionData.AdditionalOutputCheck;

            using (process = new())
            {
                process.StartInfo = processStartInfo;
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data) is false && (outputCheck?.Invoke(e.Data) ?? true))
                    {
                        if (processExecutionData.TakeLastOutputLine is true)
                            sbOutput.Clear();

                        sbOutput.AppendLine(e.Data);
                    }

                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data) is false)
                    {
                        if (processExecutionData.ReturnStandardError is true && (outputCheck?.Invoke(e.Data) ?? true))
                        {
                            if (processExecutionData.TakeLastOutputLine is true)
                                sbOutput.Clear();

                            sbOutput.AppendLine(e.Data);
                        }
                        else if (processExecutionData.ReturnStandardError is false)
                            processErrorLogs.Add(e.Data);
                    }
                };
                process.Exited += (sender, e) =>
                {
                    if (sender is Process proc)
                        exitCode = proc.ExitCode;
                };

                processStarted = process.Start();
                if (processExecutionData.ReturnStandardOutput is true && processExecutionData.ReturnStandardError is false)
                    process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            string msg = "Exception while executing subprocess.";
            processErrorLogs.Insert(0, msg);
            processErrorLogs.Insert(1, ex.Message);
            Logger.LogException(ex, "Exception while executing subprocess.", nameof(ProcessExecutor), new { processExecutionData, exitCode, processStarted });
        }

        if (cancellationTokenRegistration.Token != CancellationToken.None)
            cancellationTokenRegistration.Unregister();

        // If cancelled, just return null
        if (cancellationToken.IsCancellationRequested is true)
            return new ProcessResult<string>(null, ProcessResultStatus.Warning, "Process Cancelled");

        if (processErrorLogs.Count > 0)
        {   // Potentially double logs if exception is thrown but ensure errors are logged
            Logger.LogError(processErrorLogs, nameof(ProcessExecutor), new { processExecutionData, exitCode, processStarted });
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, "Error occurred while processing");    // Return null if error
        }

        return new ProcessResult<string>(sbOutput.ToString(), ProcessResultStatus.Success, "Successful Process Execution");
    }

    public async Task<ProcessResult<string>> ExecuteAsync(ProcessExecutionData processExecutionData, CancellationToken cancellationToken)
    {
        Process process = null;
        int? exitCode = null;
        bool processStarted = false;
        List<string> processErrorLogs = [];
        StringBuilder sbOutput = new();

        // Register on cancellation to kill the process
        CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.Register(() =>
        {
            if (processStarted is true)
                process?.Kill(true);
        });

        try
        {
            ProcessStartInfo processStartInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = processExecutionData.ReturnStandardOutput & !processExecutionData.ReturnStandardError,
                RedirectStandardError = true,
                FileName = processExecutionData.FileName,
                Arguments = processExecutionData.Arguments,
            };

            Func<string, bool> outputCheck = processExecutionData.AdditionalOutputCheck;

            using (process = new())
            {
                process.StartInfo = processStartInfo;
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data) is false && (outputCheck?.Invoke(e.Data) ?? true))
                    {
                        if (processExecutionData.TakeLastOutputLine is true)
                            sbOutput.Clear();

                        sbOutput.AppendLine(e.Data);
                    }

                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data) is false)
                    {
                        if (processExecutionData.ReturnStandardError is true && (outputCheck?.Invoke(e.Data) ?? true))
                        {
                            if (processExecutionData.TakeLastOutputLine is true)
                                sbOutput.Clear();

                            sbOutput.AppendLine(e.Data);
                        }
                        else if (processExecutionData.ReturnStandardError is false)
                            processErrorLogs.Add(e.Data);
                    }
                };
                process.Exited += (sender, e) =>
                {
                    if (sender is Process proc)
                        exitCode = proc.ExitCode;
                };

                processStarted = process.Start();
                if (processExecutionData.ReturnStandardOutput is true && processExecutionData.ReturnStandardError is false)
                    process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            string msg = "Exception while executing subprocess.";
            processErrorLogs.Insert(0, msg);
            processErrorLogs.Insert(1, ex.Message);
            Logger.LogException(ex, "Exception while executing subprocess.", nameof(ProcessExecutor), new { processExecutionData, exitCode, processStarted });
        }

        cancellationTokenRegistration.Unregister();

        // If cancelled, just return null
        if (cancellationToken.IsCancellationRequested is true)
            return new ProcessResult<string>(null, ProcessResultStatus.Warning, "Process Cancelled");

        if (processErrorLogs.Count > 0)
        {   // Potentially double logs if exception is thrown but ensure errors are logged
            Logger.LogError(processErrorLogs, nameof(ProcessExecutor), new { processExecutionData, exitCode, processStarted });
            return new ProcessResult<string>(null, ProcessResultStatus.Failure, "Error occurred while processing");    // Return null if error
        }

        return new ProcessResult<string>(sbOutput.ToString(), ProcessResultStatus.Success, "Successful Process Execution");
    }
}
