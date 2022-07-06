using AutomatedFFmpegUtilities.Data;
using System.Collections.Generic;

namespace AutomatedFFmpegClient.ViewData
{
    public class ShowSourceViewData
    {
        public string SourceName { get; set; }

        /// <summary>List of source shows.</summary>
        public List<ShowSourceData> SourceShows { get; set; }

        public ShowSourceViewData()
        {
            SourceShows = new List<ShowSourceData>();
        }
        public ShowSourceViewData(string sourceName, List<ShowSourceData> sourceShows)
        {
            SourceName = sourceName;
            SourceShows = sourceShows;
        }
    }
}
