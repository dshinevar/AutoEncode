using AutoEncodeServer.Enums;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.Managers.Interfaces;

public interface IEncodingJobManager
{
    /// <summary>Indicates if the manager is initialized.</summary>
    bool Initialized { get; }

    /// <summary>Number of jobs in queue</summary>
    int Count { get; }

    /// <summary>Sets up the manager and client update publisher</summary>
    /// <param name="shutdownMRE"><see cref="ManualResetEvent"/> used to indicate when shut down</param>
    /// <exception cref="Exception">Rethrows exceptions.</exception>
    void Initialize();

    /// <summary>Starts up threads.</summary>
    /// <exception cref="Exception">Rethrows exceptions.</exception>
    Task Run();

    /// <summary>Stops threads.</summary>
    /// <exception cref="Exception">Rethrows exceptions.</exception>
    void Shutdown();

    #region Get Requests
    /// <summary>Gets the current state of the queue</summary>
    /// <returns>List of <see cref="EncodingJobData"/></returns>
    IEnumerable<EncodingJobData> GetEncodingJobQueue();

    /// <summary>Returns the status of the encoding job (if it exists) by Guid lookup. </summary>
    /// <param name="sourceFileGuid">The guid of the source file used to try to find a linked encoding job.</param>
    /// <returns><see cref="EncodingJobStatus"/> of encoding job if found; Null, otherwise.</returns>
    EncodingJobStatus? GetEncodingJobStatusBySourceFileGuid(Guid sourceFileGuid);
    #endregion Get Requests

    #region Add Request Methods
    bool AddCreateEncodingJobRequest(ISourceFileModel sourceFile);

    bool AddRemoveEncodingJobByIdRequest(ulong id, RemovedEncodingJobReason reason);

    bool AddCancelJobByIdRequest(ulong id);

    bool AddPauseJobByIdRequest(ulong id);

    bool AddResumeJobByIdRequest(ulong id);

    bool AddPauseAndCancelJobByIdRequest(ulong id);
    #endregion Add Request Methods
}
