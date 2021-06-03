using AutomatedFFmpegUtilities.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AutomatedFFmpegClient.Model
{
    /// <summary>Model used for displaying of VideoSourceData</summary>
    public class VideoSourceViewModel
    {
        /// <summary>Name of group of source files.</summary>
        public string SourceName { get; set; }

        /// <summary>List of source files.</summary>
        public ObservableCollection<VideoSourceData> SourceFiles { get; set; }

        public VideoSourceViewModel()
        {
            SourceFiles = new ObservableCollection<VideoSourceData>();
        }
        public VideoSourceViewModel(string sourceName, List<VideoSourceData> sourceData)
        {
            SourceName = sourceName;
            SourceFiles = new ObservableCollection<VideoSourceData>(sourceData);
        }
    }
}
