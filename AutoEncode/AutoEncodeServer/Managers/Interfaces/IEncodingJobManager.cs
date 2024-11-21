using AutoEncodeServer.Enums;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Threading;

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
    void Initialize(ManualResetEvent shutdownMRE);

    /// <summary>Starts up threads.</summary>
    /// <exception cref="Exception">Rethrows exceptions.</exception>
    void Start();

    /// <summary>Stops threads.</summary>
    /// <exception cref="Exception">Rethrows exceptions.</exception>
    void Shutdown();

    #region Get Requests
    /// <summary>Gets the current state of the queue</summary>
    /// <returns>List of <see cref="EncodingJobData"/></returns>
    IEnumerable<EncodingJobData> GetEncodingJobQueue();

    /// <summary>Determines if a job exists by the given filename and is currently encoding.</summary>
    /// <param name="filename"></param>
    /// <returns>True if exists and encoding; False, otherwise.</returns>
    bool IsEncodingByFileName(string filename);

    /// <summary>Returns the status of the encoding job (if it exists) by filename lookup. </summary>
    /// <param name="filename"></param>
    /// <returns><see cref="EncodingJobStatus"/> of encoding job if found; Null, otherwise.</returns>
    EncodingJobStatus? GetEncodingJobStatusByFileName(string filename);
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
