using AutomatedFFmpegUtilities.Enums;

namespace AutomatedFFmpegUtilities.Data
{
    public class EncodingJob
    {
        public EncodingJobStatus Status { get; set; } = EncodingJobStatus.NEW;
        public bool Paused { get; set; } = false;
        public bool Cancelled { get; set; } = false;
        public string Name { get; set; }
        public string SourceFullPath { get; set; }
        public string DestinationFullPath { get; set; }

        public EncodingJob() => Status = EncodingJobStatus.NEW;
    }
}
