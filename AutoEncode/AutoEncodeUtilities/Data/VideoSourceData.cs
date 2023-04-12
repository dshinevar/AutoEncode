using AutoEncodeUtilities.Interfaces;
using System.IO;

namespace AutoEncodeUtilities.Data
{
    public class VideoSourceData
        : IUpdateable<VideoSourceData>
    {
        public string FileName => Path.GetFileName(FullPath);
        public string FullPath { get; set; }
        public bool Encoded { get; set; }

        /// <summary>Default Constructor</summary>
        public VideoSourceData() { }

        public override bool Equals(object obj)
        {
            if (obj is VideoSourceData videoSourceData)
            {
                return FullPath.Equals(videoSourceData.FullPath);
            }

            return false;
        }

        public override int GetHashCode() => FullPath.GetHashCode();

        public void Update(VideoSourceData newVideoSourceData) => newVideoSourceData.CopyProperties(this);
    }
}
