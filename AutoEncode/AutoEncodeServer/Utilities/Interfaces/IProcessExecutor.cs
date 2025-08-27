using AutoEncodeServer.Utilities.Data;
using AutoEncodeUtilities.Process;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.Utilities.Interfaces;

/// <summary>Helper class to execute sub-<see cref="Process"/>es.</summary>
public interface IProcessExecutor
{
    /// <summary>Executes and returns the process output based on the given <see cref="ProcessExecutionData"/></summary>
    /// <param name="processExecutionData">Data the describes the subprocess to be executed.</param>
    /// <param name="cancellationToken">Optional</param>
    /// <returns>
    /// <see cref="ProcessResult"/> indicating success or failure where the data is the
    /// Process output (StandardOutput or StandardError). Can be multiple lines.
    /// Returns null if error. Returns Warning if cancelled.
    /// </returns>
    /// <remarks>Recommended for fast/very short running processes.</remarks>
    ProcessResult<string> Execute(ProcessExecutionData processExecutionData, CancellationToken cancellationToken = default);

    /// <summary>Executes asynchronously and returns the process output based on the given <see cref="ProcessExecutionData"/></summary>
    /// <param name="processExecutionData">Data the describes the subprocess to be executed.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// <see cref="ProcessResult"/> indicating success or failure where the data is the
    /// Process output (StandardOutput or StandardError). Can be multiple lines.
    /// Returns null if error. Returns Warning if cancelled.
    /// </returns>
    /// <remarks>Recommended for longer running processes.</remarks>
    Task<ProcessResult<string>> ExecuteAsync(ProcessExecutionData processExecutionData, CancellationToken cancellationToken);
}
