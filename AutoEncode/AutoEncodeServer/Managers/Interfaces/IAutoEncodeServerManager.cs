using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities.Enums;
using System;
using System.Threading.Tasks;

namespace AutoEncodeServer.Managers.Interfaces;

/// <summary>
/// Interface for <see cref="AutoEncodeServerManager"/>.
/// Should really only be used by <see cref="AutoEncodeServer"/>
/// </summary>
public interface IAutoEncodeServerManager
{
    void Initialize();

    /// <summary>Starts the threads, managers, and communication services.</summary>
    /// <exception cref="Exception">Rethrows Exception</exception>
    /// <returns>Returns a <see cref="Task"/> to be awaited on until server shutdown.</returns>
    Task Run();

    /// <summary>Initiates shutdown of threads, managers, and communication services.</summary>
    void Shutdown();
}

/// <summary>Interface that allows a manager to connect to the <see cref="SourceFileManager"/></summary>
public interface ISourceFileManagerConnection
{
    bool UpdateSourceFileEncodingStatus(Guid sourceFileGuid, EncodingJobStatus? status);
}

/// <summary>Interface that allows a manager to connect to the <see cref="EncodingJobManager"> </summary>
public interface IEncodingJobManagerConnection
{
    bool CreateEncodingJob(ISourceFileModel sourceFile);

    EncodingJobStatus? GetEncodingJobStatusBySourceFileGuid(Guid sourceFileGuid);
}
