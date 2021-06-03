using AutomatedFFmpegUtilities.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace AutomatedFFmpegUtilities.Data
{
    public class SeasonSourceData : IDeepCloneable<SeasonSourceData>
    {
        public string Season { get; set; }
        public List<VideoSourceData> Episodes { get; set; }

        /// <summary>Default Constructor </summary>
        public SeasonSourceData() { }

        /// <summary>Constructor; Set SeasonNumber, creates empty list of episdoes </summary>
        /// <param name="season"></param>
        public SeasonSourceData(string season)
        {
            Season = season;
            Episodes = new List<VideoSourceData>();
        }

        /// <summary>Private Constructor used for DeepClone </summary>
        /// <param name="season"></param>
        /// <param name="episodes"></param>
        private SeasonSourceData(string season, List<VideoSourceData> episodes)
        {
            Season = season;
            Episodes = episodes.Select(e => new VideoSourceData(e)).ToList();
        }

        public SeasonSourceData DeepClone() => new SeasonSourceData(Season, Episodes);
    }
}
