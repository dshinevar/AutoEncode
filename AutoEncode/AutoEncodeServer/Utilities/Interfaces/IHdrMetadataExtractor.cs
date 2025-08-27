using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Process;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.Utilities.Interfaces;

public interface IHdrMetadataExtractor
{
    /// <summary>Runs a process to extract the hdr metadata indicated by the provided <see cref="HDRFlags"/> </summary>
    /// <param name="sourceFileFullPath">Source file to extract from.</param>
    /// <param name="hdrFlag">Type of HDR metadata to extract</param>
    /// <param name="cancellationToken">CancellationToken to stop processing early.</param>
    /// <returns><see cref="ProcessResult"/> with the full path of the outputted metadata file.</returns>
    Task<ProcessResult<string>> Extract(string sourceFileFullPath, HDRFlags hdrFlag, CancellationToken cancellationToken);
}
