using AutoEncodeUtilities.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoEncodeClient.Communication.Interfaces;

public interface ICommunicationMessageHandler
{
    /// <summary>Initializes handler -- sets IP and Port.</summary>
    void Initialize();

    void Shutdown();

    /// <summary>Requests source files from the server. </summary>
    /// <returns>Source file payload of a dictionary whose key is the Directory Name and value is a list of source files.</returns>
    Task<Dictionary<string, IEnumerable<SourceFileData>>> RequestSourceFiles();

    /// <summary>Requests the given job to be cancelled. </summary>
    /// <param name="jobId">Job to be cancelled.</param>
    /// <returns>True if request went through (not necessarily successfully cancelled).</returns>
    Task<bool> RequestCancelJob(ulong jobId);

    /// <summary>Requests the given job to be paused.</summary>
    /// <param name="jobId">Job to be paused.</param>
    /// <returns>True if request went through (not necessarily successfully paused).</returns>
    Task<bool> RequestPauseJob(ulong jobId);

    /// <summary>Requests the given job to resume. </summary>
    /// <param name="jobId">Job to be resumed.</param>
    /// <returns>True if request went through (not necessarily successfully resumed).</returns>
    Task<bool> RequestResumeJob(ulong jobId);

    /// <summary>Requests the given job to pause and cancel.</summary>
    /// <param name="jobId">Job to be paused and cancelled.</param>
    /// <returns>True if request went through (not necessarily successfully paused/cancelled).</returns>
    Task<bool> RequestPauseAndCancelJob(ulong jobId);

    /// <summary>Requests the given source file to be encoded.</summary>
    /// <param name="sourceFileGuid">Source file to be encoded.</param>
    /// <returns>True if request went through (not necessarily successfully created encoding job).</returns>
    Task<bool> RequestEncode(Guid sourceFileGuid);

    /// <summary>Requests the given source files to be encoded.</summary>
    /// <param name="sourceFileGuids">List of source files to be encoded.</param>
    /// <returns>List of source files whose request failed.</returns>
    Task<IEnumerable<string>> BulkRequestEncode(IEnumerable<Guid> sourceFileGuids);

    /// <summary>Requests for the given job to removed from the queue.</summary>
    /// <param name="jobId">Job to be removed.</param>
    /// <returns>True if request went through (not necessarily successfully removed).</returns>
    Task<bool> RequestRemoveJob(ulong jobId);

    /// <summary>Requests the current encoding job queue.</summary>
    /// <returns>List of <see cref="EncodingJobData"/>.</returns>
    Task<IEnumerable<EncodingJobData>> RequestJobQueue();
}
