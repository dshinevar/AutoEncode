using AutomatedFFmpegUtilities.Enums;
using System;
using System.IO;
using System.Linq;

namespace AutomatedFFmpegUtilities.Data
{
    public class EncodingJob
    {
        public int JobId { get; set; } = 0;
        public string Name => Path.GetFileNameWithoutExtension(FileName);
        public string FileName => Path.GetFileName(SourceFullPath);
        public string SourceFullPath { get; set; } = string.Empty;
        public string DestinationFullPath { get; set; } = string.Empty;

        #region Status
        public EncodingJobStatus Status { get; set; } = EncodingJobStatus.NEW;
        public bool Error { get; set; } = false;
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
        #endregion Status

        #region Processing Data
        public SourceStreamData SourceStreamData { get; set; }
        public EncodingInstructions EncodingInstructions { get; set; }
        public PostProcessingFlags PostProcessingFlags { get; private set; } = PostProcessingFlags.None;
        public PostProcessingSettings PostProcessingSettings { get; set; }
        public string FFmpegCommandArguments { get; set; }
        #endregion Processing Data

        #region Functions
        #region PostProcesssingFlags
        public void SetPostProcessingFlags(bool plexEnabled)
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
        public void SetPostProcessingFlag(PostProcessingFlags flag) => PostProcessingFlags |= flag;
        public void ClearPostProcessingFlag(PostProcessingFlags flag) => PostProcessingFlags &= ~flag;
        #endregion PostProcessingFlags

        public override string ToString() => $"({JobId}) {Name}";
        public void ClearError() => Error = false;
        public void SetError()
        {
            Error = true;
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
        #endregion Functions
    }
}
