using AutoEncodeServer.Utilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Process;
using System.Threading.Tasks;
using System.Threading;

namespace AutoEncodeServer.Utilities.Interfaces;

/// <summary>Processor that is used to determine info about a source file.</summary>
public interface ISourceFileProcessor
{
    /// <summary>Probes the given source file with ffprobe and produces source file info </summary>
    /// <param name="sourceFileFullPath">The source file to be probed</param>
    /// <returns><see cref="ProcessResult"/> with <see cref="SourceFileProbeResultData"/></returns>
    ProcessResult<SourceFileProbeResultData> Probe(string sourceFileFullPath);

    /// <summary>
    /// (ASYNC) Attempts to determine the <see cref="VideoScanType"/> of the given source file.
    /// </summary>
    /// <param name="sourceFileFullPath">The source file to be determined.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="ProcessResult"/> with the determined <see cref="VideoScanType"/></returns>
    Task<ProcessResult<VideoScanType>> DetermineVideoScanTypeAsync(string sourceFileFullPath, CancellationToken cancellationToken);

    /// <summary>
    /// (ASYNC) Attempts to determine the crop of the given source file. 
    /// </summary>
    /// <param name="sourceFileFullPath">The source file to be determined.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="ProcessResult"/> with the determined crop.</returns>
    /// <remarks>Returns string of crop in this format: "XXXX:YYYY:AA:BB"</remarks>
    Task<ProcessResult<string>> DetermineCropAsync(string sourceFileFullPath, CancellationToken cancellationToken);
}
