using AutomatedFFmpegUtilities.Data;
using System.Collections.Generic;

namespace AutomatedFFmpegClient.ViewData
{
    /// <summary>Model used for displaying of VideoSourceData</summary>
    public class VideoSourceViewData
    {
        /// <summary>Name of group of source files.</summary>
        public string SourceName { get; set; }

        /// <summary>List of source files.</summary>
        public List<VideoSourceData> SourceFiles { get; set; }

        public VideoSourceViewData()
        {
            SourceFiles = new List<VideoSourceData>();
        }
        public VideoSourceViewData(string sourceName, List<VideoSourceData> sourceData)
        {
            SourceName = sourceName;
            SourceFiles = new List<VideoSourceData>(sourceData);
        }
    }
}
