namespace AutomatedFFmpegUtilities.Data
{
    public class VideoSourceData
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public bool Encoded { get; set; }

        /// <summary>Default Constructor</summary>
        public VideoSourceData() { }

        /// <summary>Constructor with property setters. </summary>
        /// <param name="filename">Name of video source file.</param>
        /// <param name="encoded">Has the file been encoded.</param>
        public VideoSourceData(string filename, bool encoded)
        {
            FileName = filename;
            Encoded = encoded;
        }

        /// <summary>Copy Constructor </summary>
        /// <param name="videoSourceData"></param>
        public VideoSourceData(VideoSourceData videoSourceData)
        {
            FileName = videoSourceData.FileName;
            FullPath = videoSourceData.FullPath;
            Encoded = videoSourceData.Encoded;
        }
    }
}
