using AutomatedFFmpegUtilities.Enums;
using System.IO;

namespace AutomatedFFmpegUtilities.Data
{
    public class EncodingJob
    {
        public int JobId { get; set; } = 0;
        public string Name
        {
            get => Path.GetFileNameWithoutExtension(FileName);
        }
        public string FileName { get; set; } = string.Empty;
        public EncodingJobStatus Status { get; set; } = EncodingJobStatus.NEW;
        public bool Paused { get; set; } = false;
        public bool Cancelled { get; set; } = false;
        
        public SourceFileData SourceFileData { get; set; }
        public EncodingInstructions EncodingInstructions { get; set; }
        
        public override string ToString() => $"({JobId}) {Name}";
    }
}
