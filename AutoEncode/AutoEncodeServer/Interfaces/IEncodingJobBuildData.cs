using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;

namespace AutoEncodeServer.Interfaces
{
    /// <summary>Interface exposing necessary data/functions for building an encoding job.</summary>
    public interface IEncodingJobBuildData
    {
        /// <summary>Unique job identifier </summary>
        ulong? Id { get; }

        /// <summary>Name of job (FileName without extension) </summary>
        string Name { get; }

        /// <summary>FileName of Job </summary>
        string FileName { get; }

        /// <summary>Full Path of the job's Source File </summary>
        string SourceFullPath { get; }

        /// <summary>Directory of destination file full path </summary>
        string DestinationDirectory { get; }

        /// <summary>Full Path of the job's expected Destination File </summary>
        string DestinationFullPath { get; }

        #region Status
        /// <summary>Current substatus of the job building</summary>
        EncodingJobBuildingStatus BuildingStatus { get; }
        #endregion Status

        #region Processing Data
        /// <summary>The raw stream (video, audio subtitle) data </summary>
        ISourceStreamData SourceStreamData { get; set; }

        /// <summary>Instructions on how to encode job based on the source stream data and rules </summary>
        EncodingInstructions EncodingInstructions { get; set; }

        /// <summary>Arguments passed to FFmpeg Encoding Job </summary>
        IEncodingCommandArguments EncodingCommandArguments { get; set; }
        #endregion Processing Data

        #region Functions
        /// <summary>Sets the current status of the job.</summary>
        /// <param name="status"><see cref="EncodingJobStatus"/></param>
        void SetStatus(EncodingJobStatus status);

        /// <summary>Sets the current building substatus </summary>
        /// <param name="buildingStatus"><see cref="EncodingJobBuildingStatus"/></param>
        void SetBuildingStatus(EncodingJobBuildingStatus buildingStatus);

        /// <summary>Marks the job in error and saves the given error message; Resets Status </summary>
        /// <param name="errorMsg"></param>
        void SetError(string errorMsg);

        /// <summary>Sets the <see cref="EncodingJobStatus"/> to the previous status (unless it's marked New)</summary>
        void ResetStatus();
        #endregion Functions
    }
}
