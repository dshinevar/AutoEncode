using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;

namespace AutoEncodeServer.Models.Interfaces;

/// <summary>Interface used to access encoding job data without exposing other functionalities</summary>
public interface IEncodingJobData
{
    #region Properties
    /// <summary>Unique job identifier </summary>
    ulong Id { get; }

    /// <summary>Links this encoding job back to a source file. </summary>
    Guid SourceFileGuid { get; }

    /// <summary>Title of job -- to be used in metadata on output file</summary>
    string Title { get; }

    /// <summary>Name of job (FileName without extension) </summary>
    string Name { get; }

    /// <summary>Filename of source file </summary>
    string Filename { get; }

    /// <summary>Full Path of the job's Source File </summary>
    string SourceFullPath { get; }

    /// <summary>Full Path of the job's expected Destination File </summary>
    string DestinationFullPath { get; }
    #endregion Properties

    #region Status Properties
    /// <summary><see cref="EncodingJobStatus"/> of the job</summary>
    EncodingJobStatus Status { get; }

    /// <summary>Status / Current Step of building (<see cref="EncodingJobBuildingStatus")/></summary>
    EncodingJobBuildingStatus BuildingStatus { get; }

    /// <summary>Indicates if the job is in a processing state (Building, Encoding, or PostProcessing)</summary>
    bool IsProcessing { get; }

    /// <summary>Indicates if the job has an error </summary>
    bool HasError { get; }

    /// <summary>The current error message for the job. </summary>
    string ErrorMessage { get; }

    /// <summary>DateTime when error occurred </summary>
    DateTime? ErrorTime { get; }

    /// <summary>Indicates if the job is to be paused when done processing.</summary>
    bool ToBePaused { get; }

    /// <summary>Indicates if the job is paused.</summary>
    bool Paused { get; }

    /// <summary> Flag showing if a job is cancelled </summary>
    bool Canceled { get; }

    /// <summary>Indicates if the job is in a state to be cancelled.</summary>
    bool CanCancel { get; }

    /// <summary>Indicates if the encoding job is complete.</summary>
    bool Complete { get; }
    #endregion Status Properties

    #region Encoding Progress Properties
    /// <summary>Encoding progress percentage</summary>
    byte EncodingProgress { get; }

    /// <summary>The current fps of the encode </summary>
    double? CurrentFramesPerSecond { get; }

    /// <summary>The estimated encoding time remaining</summary>
    TimeSpan? EstimatedEncodingTimeRemaining { get; }

    /// <summary>Amount of time spent encoding.</summary>
    TimeSpan ElapsedEncodingTime { get; }

    /// <summary>DateTime when encoding was completed. </summary>
    DateTime? CompletedEncodingDateTime { get; }

    /// <summary>DateTime when postprocessing was completed.</summary>
    DateTime? CompletedPostProcessingTime { get; }
    #endregion Encoding Progress Properties

    #region Processing Data
    /// <summary>The raw stream (video, audio subtitle) data </summary>
    SourceStreamData SourceStreamData { get; }

    /// <summary>Instructions on how to encode job based on the source stream data and rules </summary>
    EncodingInstructions EncodingInstructions { get; }

    /// <summary>Settings for PostProcessing; Initially copied over from AEServerConfig file. </summary>
    PostProcessingSettings PostProcessingSettings { get; }

    /// <summary>Marks what PostProcessing functions should be done to this job. </summary>
    PostProcessingFlags PostProcessingFlags { get; }

    /// <summary>Determines if the job needs PostProcessing</summary>
    bool NeedsPostProcessing { get; }

    /// <summary>Arguments passed to FFmpeg Encoding Job </summary>
    EncodingCommandArguments EncodingCommandArguments { get; }
    #endregion ProcessingData
}
