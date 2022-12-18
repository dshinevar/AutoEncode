using System.IO;

namespace AutoEncodeUtilities.Data
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
