using AutoEncodeServer.Utilities.Interfaces;
using System;
using System.Diagnostics;

namespace AutoEncodeServer.Utilities.Data;

/// <summary>Data required to execute a subprocess by the <see cref="IProcessExecutor"/> </summary>
public class ProcessExecutionData
{
    /// <summary>Process filename -- see <see cref="ProcessStartInfo.FileName"/></summary>
    public string FileName { get; init; }

    /// <summary>Process arguments -- see <see cref="ProcessStartInfo.Arguments"/> </summary>
    public string Arguments { get; init; }

    /// <summary>
    /// Indicates that the standard output from the process should be returned.
    /// See <see cref="ProcessStartInfo.RedirectStandardOutput"/>
    /// </summary>
    public bool ReturnStandardOutput { get; init; }

    /// <summary>
    /// OVERRIDES <see cref="ReturnStandardOutput"/>.
    /// Indicates that the standard error from the process should be returned.
    /// Set to true if it is known the process utilizes standard error instead of standard output.
    /// </summary>
    public bool ReturnStandardError { get; init; }

    /// <summary>
    /// Additional check constraints on the output from the process (either StandardOutput or StandardError)
    /// to determine if it should be used.
    /// </summary>
    /// <remarks><see cref="string.IsNullOrWhiteSpace(string?)"/> is used always.</remarks>
    public Func<string, bool> AdditionalOutputCheck { get; init; }

    /// <summary>Indicates to return ONLY the last line outputted by the process.</summary>
    public bool TakeLastOutputLine { get; init; }
}
