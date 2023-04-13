using AutoEncodeUtilities.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace AutoEncodeUtilities.Data
{
    public class ShowSourceData
        //: IUpdateable<ShowSourceData>
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

        public override bool Equals(object obj)
        {
            if (obj is ShowSourceData data)
            {
                bool equals = true;
                equals &= data.ShowName == ShowName;
                equals &= data.Seasons.Count == Seasons.Count;

                if (equals is true)
                {
                    foreach (var season in data.Seasons)
                    {
                        if (Seasons.Any(x => x.Equals(season)) is false)
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
