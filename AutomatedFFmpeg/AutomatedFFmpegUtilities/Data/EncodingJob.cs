using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Interfaces;
using System;
using System.IO;
using System.Linq;

namespace AutomatedFFmpegUtilities.Data
{
    public class EncodingJob
    {
        /// <summary> Default Constructor </summary>
        public EncodingJob() { }

        /// <summary> Preferred Constructor </summary>
        /// <param name="jobId">JobId assigned by EncodingJobQueue on the server.</param>
        /// <param name="sourceFullPath">Full path of the source file.</param>
        /// <param name="destinationFullPath">Full path of the expected destination file.</param>
        /// <param name="postProcessingSettings"><see cref="PostProcessingSettings"/></param>
        public EncodingJob(int jobId, string sourceFullPath, string destinationFullPath, PostProcessingSettings postProcessingSettings)
        {
            Id = jobId;
            SourceFullPath = sourceFullPath;
            DestinationFullPath = destinationFullPath;
            PostProcessingSettings = postProcessingSettings;
            SetPostProcessingFlags();
        }

        /// <summary>Unique job identifier </summary>
        public int Id { get; } = 0;
        /// <summary>Name of job (FileName without extension) </summary>
        public string Name => Path.GetFileNameWithoutExtension(FileName);
        /// <summary>FileName of Job </summary>
        public string FileName => Path.GetFileName(SourceFullPath);
        /// <summary>Full Path of the job's Source File </summary>
        public string SourceFullPath { get; private set; } = string.Empty;
        /// <summary>Directory of destination file full path </summary>
        public string DestinationDirectory => Path.GetDirectoryName(DestinationFullPath);
        /// <summary>Full Path of the job's expected Destination File </summary>
        public string DestinationFullPath { get; private set; } = string.Empty;

        #region Status
        /// <summary>Overall Status of the Job </summary>
        public EncodingJobStatus Status { get; private set; } = EncodingJobStatus.NEW;
        /// <summary>Flag showing if a job is in error </summary>
        public bool Error { get; private set; } = false;
        /// <summary>Error message from when a job was last marked in error. </summary>
        public string LastErrorMessage { get; private set; } = string.Empty;
        /// <summary> Flag showing if a job is paused </summary>
        public bool Paused { get; set; } = false;
        /// <summary> Flag showing if a job is cancelled </summary>
        public bool Cancelled { get; set; } = false;
        private int _encodingProgress = 0;
        /// <summary>Encoding Progress Percentage </summary>
        public int EncodingProgress
        {
            get => _encodingProgress;
            set
            {
                if (value > 100) _encodingProgress = 100;
                else if (value < 0) _encodingProgress = 0;
                else _encodingProgress = value;
            }
        }
        /// <summary>Amount of time spent encoding. </summary>
        public TimeSpan? ElapsedEncodingTime { get; set; } = TimeSpan.Zero;
        /// <summary> DateTime when encoding was completed </summary>
        public DateTime? CompletedEncodingDateTime { get; private set; } = null;
        /// <summary> DateTime when postprocessing was completed </summary>
        public DateTime? CompletedPostProcessingTime { get; private set; } = null;
        /// <summary> DateTime when job was errored </summary>
        public DateTime? ErrorTime { get; private set; } = null;
        #endregion Status

        #region Processing Data
        /// <summary>The raw stream (video, audio subtitle) data </summary>
        public SourceStreamData SourceStreamData { get; set; }
        /// <summary>Instructions on how to encode job based on the source stream data and rules </summary>
        public EncodingInstructions EncodingInstructions { get; set; }
        /// <summary>Determines if the job needs PostProcessing</summary>
        public bool NeedsPostProcessing => !PostProcessingFlags.Equals(PostProcessingFlags.None) && PostProcessingSettings is not null;
        /// <summary>Marks what PostProcessing functions should be done to this job. </summary>
        public PostProcessingFlags PostProcessingFlags { get; private set; } = PostProcessingFlags.None;
        /// <summary>Settings for PostProcessing; Initially copied over from AFServerConfig file. </summary>
        public PostProcessingSettings PostProcessingSettings { get; set; }
        /// <summary>Arguments passed to FFmpeg Encoding Job </summary>
        public IEncodingCommandArguments EncodingCommandArguments { get; set; }
        #endregion Processing Data

        #region Public Functions
        public override string ToString() => $"(JobID: {Id}) {Name}";

        public void SetStatus(EncodingJobStatus status) => Status = status;
        /// <summary>Marks the job as completed encoding </summary>
        /// <param name="timeCompleted"></param>
        public void CompleteEncoding(TimeSpan timeElapsed)
        {
            SetStatus(EncodingJobStatus.ENCODED);
            CompletedEncodingDateTime = DateTime.Now;
            ElapsedEncodingTime = timeElapsed;
            EncodingProgress = 100;
        }
        /// <summary> Resets encoding status and progress </summary>
        public void ResetEncoding()
        {
            EncodingProgress = 0;
            CompletedEncodingDateTime = null;
            ElapsedEncodingTime = TimeSpan.Zero;
            SetStatus(EncodingJobStatus.BUILT);
        }
        /// <summary>Marks the job as completed post processing </summary>
        /// <param name="timeCompleted"></param>
        public void CompletePostProcessing()
        {
            SetStatus(EncodingJobStatus.POST_PROCESSED);
            CompletedPostProcessingTime = DateTime.Now;
        }
        /// <summary>Sets the given PostProcessingFlag for the job </summary>
        /// <param name="flag"><see cref="PostProcessingFlags"/></param>
        public void SetPostProcessingFlag(PostProcessingFlags flag) => PostProcessingFlags |= flag;
        /// <summary>Clears the job of the given PostProcessingFlag </summary>
        /// <param name="flag"><see cref="PostProcessingFlags"/></param>
        public void ClearPostProcessingFlag(PostProcessingFlags flag) => PostProcessingFlags &= ~flag;
        /// <summary>Clears the job of error and wipes error message. </summary>
        public void ClearError()
        {
            Error = false;
            LastErrorMessage = string.Empty;
            ErrorTime = null;
        }
        /// <summary>Marks the job in error and saves the given error message; Resets Status </summary>
        /// <param name="errorMsg"></param>
        public void SetError(string errorMsg)
        {
            Error = true;
            LastErrorMessage = errorMsg;
            ErrorTime = DateTime.Now;

            switch (Status)
            {
                case EncodingJobStatus.ENCODING:
                {
                    ResetEncoding();
                    break;
                }
                default:
                {
                    ResetStatus();
                    break;
                }
            }
        }

        /// <summary>Sets the <see cref="EncodingJobStatus"/> to the previous status (unless it's marked New)</summary>
        public void ResetStatus()
        {
            if (Status > EncodingJobStatus.NEW)
            {
                Status -= 1;
            }
        }
        #endregion Public Functions

        #region Private Functions
        private void SetPostProcessingFlags()
        {
            if (PostProcessingSettings is null)
            {
                PostProcessingFlags = PostProcessingFlags.None;
                return;
            }

            if ((PostProcessingSettings?.CopyFilePaths?.Any() ?? false) is true)
            {
                SetPostProcessingFlag(PostProcessingFlags.Copy);
            }
            else
            {
                ClearPostProcessingFlag(PostProcessingFlags.Copy);
            }

            if ((PostProcessingSettings?.DeleteSourceFile ?? false) is true)
            {
                SetPostProcessingFlag(PostProcessingFlags.DeleteSourceFile);
            }
            else
            {
                ClearPostProcessingFlag(PostProcessingFlags.DeleteSourceFile);
            }
        }
        #endregion Private Functions
    }
}
