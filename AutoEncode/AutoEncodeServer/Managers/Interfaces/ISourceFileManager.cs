using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AutoEncodeServer.Managers.Interfaces;

public interface ISourceFileManager
{
    #region Init / Start / Stop
    /// <summary>Initializes the source file manager.</summary>
    /// <param name="shutdownMRE"><see cref="ManualResetEvent"/> used to signal when the source file manager has fully shutdown.</param>
    void Initialize(ManualResetEvent shutdownMRE);

    /// <summary>Starts all source file manager threads.</summary>
    void Start();

    /// <summary>Stops all source file manager threads.</summary>
    void Stop();
    #endregion Init / Start / Stop

    #region Requests
    /// <summary>Gets all source files -- groups by their DirectoryName.</summary>
    /// <returns>Dictionary where the key is the directory name and values are a list of <see cref="SourceFileData"/></returns>
    Dictionary<string, IEnumerable<SourceFileData>> RequestSourceFiles();

    /// <summary>Requests an encoding job is created for the given source file guid.</summary>
    /// <param name="sourceFileGuid"><see cref="Guid"/> for the source file.</param>
    /// <returns>True if request was added.</returns>
    bool RequestEncodingJob(Guid sourceFileGuid);

    /// <summary>Requests an encoding job for every supplied source file Guid.</summary>
    /// <param name="sourceFileGuids">List of Guids for the source files.</param>
    /// <returns>List of filenames for jobs that were not requested.</returns>
    IEnumerable<string> BulkRequestEncodingJob(IEnumerable<Guid> sourceFileGuids);

    /// <summary>Adds a request to update the source file encoding status to the processing queue.</summary>
    /// <param name="sourceFileGuid"><see cref="Guid"/> for the source file.</param>
    /// <param name="encodingJobStatus">Status of encoding job to be translated to <see cref="SourceFileEncodingStatus"/></param>
    /// <returns>True if added to process queue.</returns>
    bool AddUpdateSourceFileEncodingStatusRequest(Guid sourceFileGuid, EncodingJobStatus encodingJobStatus);
    #endregion Requests
}
