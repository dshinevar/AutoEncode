using AutoEncodeUtilities.Interfaces;
using System;
using System.IO;

namespace AutoEncodeUtilities.Data
{
    public class VideoSourceData :
        IUpdateable<VideoSourceData>,
        IEquatable<VideoSourceData>
    {
        public string FileName => Path.GetFileName(FullPath);
        public string FullPath { get; set; }
        public bool Encoded { get; set; }

        /// <summary>Default Constructor</summary>
        public VideoSourceData() { }

        public bool Equals(VideoSourceData data) => FullPath.Equals(data.FullPath);
        public override bool Equals(object obj)
        {
            if (obj is VideoSourceData videoSourceData) 
            {
                return Equals(videoSourceData);
            }

            return false;
        }

        public override int GetHashCode() => FullPath.GetHashCode();

        public void Update(VideoSourceData newVideoSourceData) => newVideoSourceData.CopyProperties(this);

        public static int CompareByFileName(VideoSourceData data1, VideoSourceData data2) => string.Compare(data1.FullPath, data2.FullPath);

        public static int CompareByFullPath(VideoSourceData data1, VideoSourceData data2) => string.Compare(data1.FullPath, data2.FullPath);
    }
}
