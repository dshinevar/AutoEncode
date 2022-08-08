using System.Collections.Generic;
using System.Linq;

namespace AutomatedFFmpegUtilities.Data
{
    public class ShowSourceData
    {
        public string ShowName { get; set; }
        public List<SeasonSourceData> Seasons { get; set; } = new List<SeasonSourceData>();

        /// <summary>Default Constructor </summary>
        public ShowSourceData() { }

        /// <summary>Constructor; Sets ShowName, creates empty season list. </summary>
        /// <param name="showName"></param>
        public ShowSourceData(string showName)
        {
            ShowName = showName;
            Seasons = new List<SeasonSourceData>();
        }

        /// <summary>Private Constructor used for DeepClone</summary>
        /// <param name="show"></param>
        /// <param name="seasons"></param>
        private ShowSourceData(string show, List<SeasonSourceData> seasons)
        {
            ShowName = show;
            Seasons = seasons.Select(s => s.DeepClone()).ToList();
        }
    }
}
