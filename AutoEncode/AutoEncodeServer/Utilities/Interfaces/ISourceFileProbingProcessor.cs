using AutoEncodeServer.Utilities.Data;
using AutoEncodeUtilities.Process;

namespace AutoEncodeServer.Utilities.Interfaces;

public interface ISourceFileProbingProcessor
{
    /// <summary>Probes the given source file with ffprobe and produces source file info </summary>
    /// <param name="sourceFileFullPath">The source file to be probed</param>
    /// <returns><see cref="ProcessResult"/> with <see cref="SourceFileProbeResultData"/></returns>
    ProcessResult<SourceFileProbeResultData> Probe(string sourceFileFullPath);
}
