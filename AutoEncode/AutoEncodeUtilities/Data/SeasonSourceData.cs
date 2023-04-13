using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoEncodeUtilities.Data
{
    public class SeasonSourceData
    {
        public string Season { get; set; }
        public int SeasonInt => Season.Contains("Special") ? 0 : Convert.ToInt32(Season.Replace("Season", string.Empty).Trim());
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

        public override bool Equals(object obj)
        {
            if (obj is SeasonSourceData data) 
            {
                bool equals = true;
                equals &= data.Season.Equals(Season);
                equals &= data.Episodes.Count == data.Episodes.Count;

                if (equals is true) 
                {
                    foreach (var episode in data.Episodes)
                    {
                        if (Episodes.Any(x => x.Equals(episode)) is false)
                        {
                            equals = false;
                            break;
                        }
                    }
                }

                return equals;
            }
            return false;
        }
    }
}
