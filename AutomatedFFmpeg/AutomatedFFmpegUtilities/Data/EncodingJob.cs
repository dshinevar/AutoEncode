using AutomatedFFmpegUtilities.Enums;

namespace AutomatedFFmpegUtilities.Data
{
    public class EncodingJob
    {
        public int JobId { get; set; } = 0;
        public EncodingJobStatus Status { get; set; } = EncodingJobStatus.NEW;
        public bool Paused { get; set; } = false;
        public bool Cancelled { get; set; } = false;
        public string FileName { get; set; } = string.Empty;
        public string SourceFullPath { get; set; } = string.Empty;
        public string DestinationFullPath { get; set; } = string.Empty;
        public SourceFileData SourceFileData { get; set; } = null;
        
        public override string ToString() => $"({JobId}) {FileName}";
    }
}
