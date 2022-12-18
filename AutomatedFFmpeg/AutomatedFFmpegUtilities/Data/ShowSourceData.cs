using System.Collections.Generic;

namespace AutoEncodeUtilities.Data
{
    public class ShowSourceData
    {
        public string ShowName { get; set; }
        public List<SeasonSourceData> Seasons { get; set; }

        /// <summary>Default Constructor </summary>
        public ShowSourceData() { }

        /// <summary>Constructor; Sets ShowName, creates empty season list. </summary>
        /// <param name="showName"></param>
        public ShowSourceData(string showName)
        {
            ShowName = showName;
            Seasons = new List<SeasonSourceData>();
        }
    }
}
