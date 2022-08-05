using AutomatedFFmpegUtilities.Enums;
using System.IO;

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
        #endregion Status

        public SourceStreamData SourceStreamData { get; set; }
        public EncodingInstructions EncodingInstructions { get; set; }
        public string FFmpegCommandArguments { get; set; }

        #region Functions
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
                default: break;
            }
        }
        #endregion Functions
    }
}
