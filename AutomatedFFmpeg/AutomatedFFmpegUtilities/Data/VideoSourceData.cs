using System.IO;

namespace AutomatedFFmpegUtilities.Data
{
    public class VideoSourceData
    {
        public string FileName => Path.GetFileName(FullPath);
        public string FullPath { get; set; }
        public bool Encoded { get; set; }

        /// <summary>Default Constructor</summary>
        public VideoSourceData() { }
    }
}
