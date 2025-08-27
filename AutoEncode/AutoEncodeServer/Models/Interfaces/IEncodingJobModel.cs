using AutoEncodeServer.Models.Data;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.Models.Interfaces;

public interface IEncodingJobModel : IEncodingJobData
{
    event EventHandler<EncodingJobStatusChangedEventArgs> EncodingJobStatusChanged;

    #region Processing Methods
    /// <summary>Builds out the encoding job -- determines source file info and builds encoding instructions.</summary>
    /// <param name="cancellationToken">TokenSource used for cancelling the build. The model holds onto the source.</param>
    Task Build(CancellationTokenSource cancellationTokenSource);

    /// <summary>Encodes the job -- will determine if needs to do dolby vision encoding or normal encoding.</summary>
    /// <param name="cancellationTokenSource">TokenSource used for cancelling the build. The model holds onto the source.</param>
    void Encode(CancellationTokenSource cancellationTokenSource);

    /// <summary>Does the post-processing for the encoding job. </summary>
    /// <param name="cancellationTokenSource">TokenSource used for cancelling the build. The model holds onto the source.</param>
    void PostProcess(CancellationTokenSource cancellationTokenSource);

    /// <summary>Cleans up the encoding job after processing (handles cancel/pause).</summary>
    void CleanupJob();
    #endregion Processing Methods
    #region Action Methods
    /// <summary>If able, calls Cancel on job's cancellation token. </summary>
    void Cancel();

    /// <summary>Marks the job paused or to be paused if currently processing</summary>
    void Pause();

    /// <summary>Unpauses the job.</summary>
    void Resume();
    #endregion Action Methods

    EncodingJobData ToEncodingJobData();
}
