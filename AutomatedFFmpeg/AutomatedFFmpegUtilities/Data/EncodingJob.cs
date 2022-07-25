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
        public EncodingJobStatus Status { get; set; } = EncodingJobStatus.NEW;
        public bool Paused { get; set; } = false;
        public bool Cancelled { get; set; } = false;
        public int EncodingProgress { get; set; } = 0;
        
        public SourceStreamData SourceStreamData { get; set; }
        public EncodingInstructions EncodingInstructions { get; set; }
        public string FfmpegCommandArguments { get; set; }
        
        public override string ToString() => $"({JobId}) {Name}";
    }
}
