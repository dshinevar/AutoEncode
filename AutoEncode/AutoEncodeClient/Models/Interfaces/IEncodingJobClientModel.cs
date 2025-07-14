using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AutoEncodeClient.Models.Interfaces;

public interface IEncodingJobClientModel : INotifyPropertyChanged
{
    /// <summary>Unique job identifier </summary>
    ulong Id { get; }

    /// <summary>Title of job</summary>
    string Title { get; }

    /// <summary>Name of job (FileName without extension) </summary>
    string Name { get; }

    /// <summary>FileName of Job </summary>
    string FileName { get; }

    /// <summary>Full Path of the job's Source File </summary>
    string SourceFullPath { get; }

    /// <summary>Full Path of the job's expected Destination File </summary>
    string DestinationFullPath { get; }

    #region Status
    /// <summary>Overall Status of the Job </summary>
    EncodingJobStatus Status { get; }

    /// <summary>Building substatus</summary>
    EncodingJobBuildingStatus BuildingStatus { get; }

    /// <summary>Flag showing if a job is in error </summary>
    bool HasError { get; }

    /// <summary>Current error message for job. </summary>
    string ErrorMessage { get; }

    /// <summary> DateTime when job was errored </summary>
    DateTime? ErrorTime { get; }

    /// <summary> Flag showing a job is to be paused.</summary>
    bool ToBePaused { get; }

    /// <summary> Flag showing if a job is paused </summary>
    bool Paused { get; }

    /// <summary> Flag showing if a job is canceled </summary>
    bool Canceled { get; }

    /// <summary>Shows if the job is in a state that can be cancelled.</summary>
    bool CanCancel { get; }

    /// <summary>Encoding Progress Percentage </summary>
    byte EncodingProgress { get; }

    /// <summary>Current frames per second of the encode. </summary>
    double? CurrentFramesPerSecond { get; }

    /// <summary>Estimated time remaining from encoding. </summary>
    TimeSpan? EstimatedEncodingTimeRemaining { get; }

    /// <summary>Amount of time spent encoding. </summary>
    TimeSpan ElapsedEncodingTime { get; }

    /// <summary> DateTime when encoding was completed </summary>
    DateTime? CompletedEncodingDateTime { get; }

    /// <summary> DateTime when postprocessing was completed </summary>
    DateTime? CompletedPostProcessingTime { get; }

    /// <summary>Flag showing if job is fully complete. </summary>
    bool Complete { get; }
    #endregion Status

    #region Processing Data
    /// <summary>The raw stream (video, audio subtitle) data </summary>
    SourceStreamData SourceStreamData { get; }

    /// <summary>Instructions on how to encode job based on the source stream data and rules </summary>
    EncodingInstructions EncodingInstructions { get; }

    /// <summary>Determines if the job needs PostProcessing</summary>
    bool NeedsPostProcessing { get; }

    /// <summary>Marks what PostProcessing functions should be done to this job. </summary>
    PostProcessingFlags PostProcessingFlags { get; }

    /// <summary>Settings for PostProcessing; Initially copied over from AEServerConfig file. </summary>
    PostProcessingSettings PostProcessingSettings { get; }

    /// <summary>Arguments passed to FFmpeg Encoding Job </summary>
    EncodingCommandArguments EncodingCommandArguments { get; }
    #endregion Processing Data

    /// <summary>Requests a cancel of the current job's operation.</summary>
    /// <returns>True if successful</returns>
    Task<bool> Cancel();

    /// <summary>Requests a pause of the job</summary>
    /// <returns>True if successful</returns>
    Task<bool> Pause();

    /// <summary>Requests a resume of the job</summary>
    /// <returns>True if successful</returns>
    Task<bool> Resume();

    /// <summary>Requests a cancel then pause of the job</summary>
    /// <returns>True if successful</returns></returns>
    Task<bool> CancelThenPause();


    /// <summary>Request job removed from queue</summary>
    /// <returns>True if successful</returns>
    Task<bool> Remove();
}
