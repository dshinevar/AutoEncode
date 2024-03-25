using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Json;
using Newtonsoft.Json;
using System;

namespace AutoEncodeUtilities.Interfaces
{
    /// <summary>Interface that inidicates what any Encoding Job object should have</summary>
    public interface IEncodingJobData
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
        EncodingJobStatus Status { get; }
        EncodingJobBuildingStatus BuildingStatus { get; }
        /// <summary>Flag showing if a job is in error </summary>
        bool HasError { get; }
        /// <summary>Error message from when a job was last marked in error. </summary>
        string ErrorMessage { get; }
        /// <summary> Flag showing a job is to be paused.</summary>
        bool ToBePaused { get; }
        /// <summary> Flag showing if a job is paused </summary>
        bool Paused { get; }
        /// <summary> Flag showing if a job is cancelled </summary>
        bool Canceled { get; }
        /// <summary>Shows if the job is in a state that can be cancelled.</summary>
        bool CanCancel { get; }
        /// <summary>Encoding Progress Percentage </summary>
        byte EncodingProgress { get; }
        /// <summary>The current fps of the encode </summary>
        double? CurrentFramesPerSecond { get; }
        /// <summary>The estimated encoding time remaining</summary>
        TimeSpan? EstimatedEncodingTimeRemaining { get; }
        /// <summary>Amount of time spent encoding. </summary>
        TimeSpan ElapsedEncodingTime { get; }
        /// <summary> DateTime when encoding was completed </summary>
        DateTime? CompletedEncodingDateTime { get; }
        /// <summary> DateTime when postprocessing was completed </summary>
        DateTime? CompletedPostProcessingTime { get; }
        /// <summary> DateTime when job was errored </summary>
        DateTime? ErrorTime { get; }
        /// <summary>Flag showing if job is fully complete </summary>
        bool Complete { get; }
        #endregion Status

        #region Processing Data
        /// <summary>The raw stream (video, audio subtitle) data </summary>
        ISourceStreamData SourceStreamData { get; }
        /// <summary>Instructions on how to encode job based on the source stream data and rules </summary>
        EncodingInstructions EncodingInstructions { get; }
        /// <summary>Determines if the job needs PostProcessing</summary>
        bool NeedsPostProcessing { get; }
        /// <summary>Marks what PostProcessing functions should be done to this job. </summary>
        PostProcessingFlags PostProcessingFlags { get; }
        /// <summary>Settings for PostProcessing; Initially copied over from AEServerConfig file. </summary>
        PostProcessingSettings PostProcessingSettings { get; }
        /// <summary>Arguments passed to FFmpeg Encoding Job </summary>
        [JsonConverter(typeof(EncodingCommandArgumentsConverter<IEncodingCommandArguments>))]
        IEncodingCommandArguments EncodingCommandArguments { get; }
        #endregion Processing Data
    }
}
