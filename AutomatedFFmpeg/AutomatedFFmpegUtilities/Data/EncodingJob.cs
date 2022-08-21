using AutomatedFFmpegUtilities.Enums;
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
        /// <param name="plexEnabled">Is Plex functionality enabled; Determines PostProcessingFlags override</param>
        public EncodingJob(int jobId, string sourceFullPath, string destinationFullPath, PostProcessingSettings postProcessingSettings, bool plexEnabled)
        {
            JobId = jobId;
            SourceFullPath = sourceFullPath;
            DestinationFullPath = destinationFullPath;
            PostProcessingSettings = postProcessingSettings;
            SetPostProcessingFlags(plexEnabled);
        }

        public int JobId { get; set; } = 0;
        public string Name => Path.GetFileNameWithoutExtension(FileName);
        public string FileName => Path.GetFileName(SourceFullPath);
        public string SourceFullPath { get; set; } = string.Empty;
        public string DestinationFullPath { get; set; } = string.Empty;

        #region Status
        public EncodingJobStatus Status { get; set; } = EncodingJobStatus.NEW;
        public bool Error { get; set; } = false;
        public string LastErrorMessage { get; set; } = string.Empty;
        public bool Paused { get; set; } = false;
        public bool Cancelled { get; set; } = false;
        private int _encodingProgress = 0;
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
        public DateTime? CompletedEncodingDateTime { get; set; } = null;
        public DateTime? CompletedPostProcessingTime { get; set; } = null;
        public DateTime? ErrorTime { get; set; } = null;
        #endregion Status

        #region Processing Data
        /// <summary>The raw stream (video, audio subtitle) data </summary>
        public SourceStreamData SourceStreamData { get; set; }
        /// <summary>Instructions on how to encode job based on the source stream data and rules </summary>
        public EncodingInstructions EncodingInstructions { get; set; }
        /// <summary>Marks what PostProcessing functions should be done to this job. </summary>
        public PostProcessingFlags PostProcessingFlags { get; private set; } = PostProcessingFlags.None;
        /// <summary>Settings for PostProcessing; Initially copied over from AFServerConfig file. </summary>
        public PostProcessingSettings PostProcessingSettings { get; set; }
        /// <summary>Arguments passed to FFmpeg Encoding Job </summary>
        public string FFmpegEncodingCommandArguments { get; set; }
        #endregion Processing Data

        #region Public Functions
        public override string ToString() => $"({JobId}) {Name}";
        public void SetPostProcessingFlag(PostProcessingFlags flag) => PostProcessingFlags |= flag;
        public void ClearPostProcessingFlag(PostProcessingFlags flag) => PostProcessingFlags &= ~flag;
        public void ClearError()
        {
            Error = false;
            LastErrorMessage = string.Empty;
            ErrorTime = null;
        }
        public void SetError(string errorMsg)
        {
            Error = true;
            LastErrorMessage = errorMsg;
            ErrorTime = DateTime.Now;
            if (Status.Equals(EncodingJobStatus.ENCODING)) EncodingProgress = 0;
            ResetStatus();
        }

        public void ResetStatus()
        {
            switch (Status)
            {
                case EncodingJobStatus.BUILDING:
                {
                    Status = EncodingJobStatus.NEW;
                    break;
                }
                case EncodingJobStatus.ENCODING:
                {
                    Status = EncodingJobStatus.BUILT;
                    break;
                }
                case EncodingJobStatus.POST_PROCESSING:
                {
                    Status = EncodingJobStatus.ENCODED;
                    break;
                }
                default: break;
            }
        }
        #endregion Public Functions

        #region Private Functions
        private void SetPostProcessingFlags(bool plexEnabled)
        {
            if (PostProcessingSettings is null)
            {
                PostProcessingFlags = PostProcessingFlags.None;
                return;
            }

            if ((PostProcessingSettings.CopyFilePaths?.Any() ?? false) is true)
            {
                SetPostProcessingFlag(PostProcessingFlags.Copy);
            }
            else
            {
                ClearPostProcessingFlag(PostProcessingFlags.Copy);
            }

            if (plexEnabled is true)
            {
                if (string.IsNullOrWhiteSpace(PostProcessingSettings.PlexLibraryName) is false)
                {
                    SetPostProcessingFlag(PostProcessingFlags.PlexLibraryUpdate);
                }
                else
                {
                    ClearPostProcessingFlag(PostProcessingFlags.PlexLibraryUpdate);
                }
            }
            else
            {
                ClearPostProcessingFlag(PostProcessingFlags.PlexLibraryUpdate);
            }

            if (PostProcessingSettings.DeleteSourceFile is true)
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
