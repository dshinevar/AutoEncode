using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.Interfaces
{
    public interface IEncodingJobModel
    {
        event PropertyChangedEventHandler PropertyChanged;

        #region Properties
        /// <summary>Unique job identifier </summary>
        ulong Id { get; }

        /// <summary>Title of job -- to be used in metadata on output file</summary>
        string Title { get; }

        /// <summary>Name of job (FileName without extension) </summary>
        string Name { get; }

        /// <summary>Filename of source file </summary>
        string FileName { get; }

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

        /// <summary>Amount of time spent encoding.</summary>
        TimeSpan ElapsedEncodingTime { get; }

        /// <summary>DateTime when encoding was completed. </summary>
        DateTime? CompletedEncodingDateTime { get; }

        /// <summary>DateTime when postprocessing was completed.</summary>
        DateTime? CompletedPostProcessingTime { get; }
        #endregion Encoding Progress Properties

        #region Processing Data
        /// <summary>The raw stream (video, audio subtitle) data </summary>
        ISourceStreamData SourceStreamData { get; }

        /// <summary>Instructions on how to encode job based on the source stream data and rules </summary>
        EncodingInstructions EncodingInstructions { get; }

        /// <summary>Settings for PostProcessing; Initially copied over from AEServerConfig file. </summary>
        PostProcessingSettings PostProcessingSettings { get; }

        /// <summary>Marks what PostProcessing functions should be done to this job. </summary>
        PostProcessingFlags PostProcessingFlags { get; }

        /// <summary>Determines if the job needs PostProcessing</summary>
        bool NeedsPostProcessing { get; }

        /// <summary>Arguments passed to FFmpeg Encoding Job </summary>
        IEncodingCommandArguments EncodingCommandArguments { get; }
        #endregion ProcessingData

        #region Methods
        /// <summary>Sets the current status of the job.</summary>
        /// <param name="status"><see cref="EncodingJobStatus"/></param>
        void SetStatus(EncodingJobStatus status);

        /// <summary>Sets the current building substatus </summary>
        /// <param name="buildingStatus"><see cref="EncodingJobBuildingStatus"/></param>
        void SetBuildingStatus(EncodingJobBuildingStatus buildingStatus);

        /// <summary>Marks the job in error and saves the given error message; Resets Status </summary>
        /// <param name="errorMsg"></param>
        void SetError(string errorMessage);

        /// <summary>If able, calls Cancel on job's cancellation token. </summary>
        void Cancel();

        /// <summary>Resets job after cancel is complete.</summary>
        void ResetCancel();

        /// <summary>Marks the job paused or to be paused if currently processing</summary>
        void Pause();

        /// <summary>Unpauses the job.</summary>
        void Resume();

        /// <summary>Sets the SourceStreamData on the model.</summary>
        /// <param name="sourceStreamData"></param>
        void SetSourceStreamData(ISourceStreamData sourceStreamData);

        /// <summary>Sets the <see cref="VideoScanType"/> of the source video data. </summary>
        /// <param name="scanType"><see cref="VideoScanType"/></param>
        void SetSourceScanType(VideoScanType scanType);

        /// <summary>Sets the crop of the source video data.</summary>
        /// <param name="crop">Crop string</param>
        void SetSourceCrop(string crop);

        /// <summary>Adds the file path for the created hdr metadata file</summary>
        /// <param name="hdrFlag"><see cref="HDRFlags"/></param>
        /// <param name="hdrMetadataFilePath">File path of metadata file</param>
        void AddSourceHDRMetadataFilePath(HDRFlags hdrFlag, string hdrMetadataFilePath);

        /// <summary>Sets the EncodingInstructions</summary>
        /// <param name="encodingInstructions"><see cref="EncodingInstructions"/></param>
        void SetEncodingInstructions(EncodingInstructions encodingInstructions);

        /// <summary>Sets the encoding command arguments.</summary>
        /// <param name="encodingCommandArguments"></param>
        void SetEncodingCommandArguments(IEncodingCommandArguments encodingCommandArguments);

        /// <summary>Updates the encoding progress percentage and time elapsed</summary>
        /// <param name="progress">Encoding Progress Percentage</param>
        /// <param name="timeElapsed">Encoding Time Elapsed.</param>
        void UpdateEncodingProgress(byte? progress, TimeSpan? timeElapsed);

        /// <summary>Marks the job as completed encoding </summary>
        /// <param name="timeElapsed"></param>
        void CompleteEncoding(TimeSpan timeElapsed);

        /// <summary>Marks the job as completed post processing </summary>
        void CompletePostProcessing();

        /// <summary>Sets the cancellation token source.</summary>
        /// <param name="cancellationTokenSource"></param>
        void SetTaskCancellationToken(CancellationTokenSource cancellationTokenSource);

        /// <summary>Gets <see cref="EncodingJobStatusUpdateData"/></summary>
        /// <returns><see cref="EncodingJobStatusUpdateData"/></returns>
        EncodingJobStatusUpdateData GetStatusUpdate();

        /// <summary>Gets <see cref="EncodingJobProcessingDataUpdateData"/> </summary>
        /// <returns><see cref="EncodingJobProcessingDataUpdateData"/></returns>
        EncodingJobProcessingDataUpdateData GetProcessingDataUpdate();

        /// <summary>Gets <see cref="EncodingJobEncodingProgressUpdateData"/></summary>
        /// <returns><see cref="EncodingJobEncodingProgressUpdateData"/></returns>
        EncodingJobEncodingProgressUpdateData GetEncodingUpdate();

        EncodingJobData ToEncodingJobData();
        #endregion Methods

    }
}
