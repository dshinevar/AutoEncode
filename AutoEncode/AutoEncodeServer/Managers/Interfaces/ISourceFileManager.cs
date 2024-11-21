using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AutoEncodeServer.Managers.Interfaces;

public interface ISourceFileManager
{
    bool Initialized { get; }

    #region Init / Start / Stop
    /// <summary>Initializes the source file manager.</summary>
    /// <param name="shutdownMRE"><see cref="ManualResetEvent"/> used to signal when the source file manager has fully shutdown.</param>
    void Initialize(ManualResetEvent shutdownMRE);

    /// <summary>Starts all source file manager threads.</summary>
    void Start();

    /// <summary>Stops all source file manager threads.</summary>
    void Shutdown();
    #endregion Init / Start / Stop

    #region Requests
    /// <summary>Gets all source files -- groups by their DirectoryName.</summary>
    /// <returns>Dictionary where the key is the directory name and values are a list of <see cref="SourceFileData"/></returns>
    Dictionary<string, IEnumerable<SourceFileData>> RequestSourceFiles();

    /// <summary>Adds a request to update the source file encoding status to the processing queue.</summary>
    /// <param name="sourceFileGuid"><see cref="Guid"/> for the source file.</param>
    /// <param name="encodingJobStatus">Status of encoding job to be translated to <see cref="SourceFileEncodingStatus"/></param>
    /// <returns>True if added to process queue.</returns>
    bool AddUpdateSourceFileEncodingStatusRequest(Guid sourceFileGuid, EncodingJobStatus encodingJobStatus);

    bool AddRequestEncodingJobForSourceFileRequest(Guid sourceFileGuid);

    bool AddBulkRequestEncodingJobRequest(IEnumerable<Guid> sourceFileGuids);
    #endregion Requests
}
