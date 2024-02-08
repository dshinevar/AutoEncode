using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using AutoEncodeUtilities.Json;
using Newtonsoft.Json;
using System;
using System.IO;

namespace AutoEncodeUtilities.Data
{
    public class EncodingJobData : IEncodingJobData
    {
        /// <summary>Unique job identifier </summary>
        public ulong Id { get; set; }
        /// <summary>Title of job</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Name of job (FileName without extension) </summary>
        public string Name => Path.GetFileNameWithoutExtension(FileName);
        /// <summary>FileName of Job </summary>
        public string FileName => Path.GetFileName(SourceFullPath);
        /// <summary>Full Path of the job's Source File </summary>
        public string SourceFullPath { get; set; } = string.Empty;
        /// <summary>Directory of destination file full path </summary>
        public string DestinationDirectory => Path.GetDirectoryName(DestinationFullPath);
        /// <summary>Full Path of the job's expected Destination File </summary>
        public string DestinationFullPath { get; set; } = string.Empty;

        #region Status
        /// <summary>Overall Status of the Job </summary>
        public EncodingJobStatus Status { get; set; } = EncodingJobStatus.NEW;
        /// <summary>Building SubStatus</summary>
        public EncodingJobBuildingStatus BuildingStatus { get; set; } = EncodingJobBuildingStatus.BUILDING;
        /// <summary>Flag showing if a job is in error </summary>
        public bool HasError { get; set; } = false;
        /// <summary>Error message from when a job was last marked in error. </summary>
        public string ErrorMessage { get; set; } = string.Empty;
        /// <summary> Flag showing a job is to be paused.</summary>
        public bool ToBePaused { get; set; } = false;
        /// <summary> Flag showing if a job is paused </summary>
        public bool Paused { get; set; } = false;
        /// <summary> Flag showing if a job is cancelled </summary>
        public bool Canceled { get; set; } = false;
        /// <summary>Shows if the job is in a state that can be cancelled.</summary>
        public bool CanCancel { get; set; } = false;
        /// <summary>Encoding Progress Percentage </summary>
        public byte EncodingProgress { get; set; }
        /// <summary>Amount of time spent encoding. </summary>
        public TimeSpan ElapsedEncodingTime { get; set; } = TimeSpan.Zero;
        /// <summary> DateTime when encoding was completed </summary>
        public DateTime? CompletedEncodingDateTime { get; set; } = null;
        /// <summary> DateTime when postprocessing was completed </summary>
        public DateTime? CompletedPostProcessingTime { get; set; } = null;
        /// <summary> DateTime when job was errored </summary>
        public DateTime? ErrorTime { get; set; } = null;
        /// <summary>Flag for showing if a job is fully completed. </summary>
        public bool Complete { get; set; } = false;
        #endregion Status

        #region Processing Data
        /// <summary>The raw stream (video, audio subtitle) data </summary>
        public ISourceStreamData SourceStreamData { get; set; }
        /// <summary>Instructions on how to encode job based on the source stream data and rules </summary>
        public EncodingInstructions EncodingInstructions { get; set; }
        /// <summary>Determines if the job needs PostProcessing</summary>
        public bool NeedsPostProcessing => !PostProcessingFlags.Equals(PostProcessingFlags.None) && PostProcessingSettings is not null;
        /// <summary>Marks what PostProcessing functions should be done to this job. </summary>
        public PostProcessingFlags PostProcessingFlags { get; set; } = PostProcessingFlags.None;
        /// <summary>Settings for PostProcessing; Initially copied over from AEServerConfig file. </summary>
        public PostProcessingSettings PostProcessingSettings { get; set; }
        /// <summary>Arguments passed to FFmpeg Encoding Job </summary>
        [JsonConverter(typeof(EncodingCommandArgumentsConverter<IEncodingCommandArguments>))]
        public IEncodingCommandArguments EncodingCommandArguments { get; set; }
        #endregion Processing Data

        public EncodingJobData() { }

        public override bool Equals(object obj)
        {
            if (obj is IEncodingJobData data)
            {
                return Id == data.Id;
            }

            return false;
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}
