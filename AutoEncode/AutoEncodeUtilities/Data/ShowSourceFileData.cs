using AutoEncodeUtilities.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoEncodeUtilities.Data
{
    /// <summary>Class derived from <see cref="SourceFileData"/> that adds on show specific data (Show Name, Season, Episode). </summary>
    public class ShowSourceFileData :
        SourceFileData,
        ISourceFileData
    {
        #region Properties
        public string ShowName { get; set; }

        public string Season => $"Season {SeasonInt}";

        public int SeasonInt { get; set; }

        public string EpisodeName { get; set; }

        public IEnumerable<int> EpisodeInts { get; set; }    // Could span multiple episodes
        #endregion Properties

        public ShowSourceFileData() : base() { }

        public ShowSourceFileData(ISourceFileData sourceFileData) : base(sourceFileData)
        {
            try
            {
                // [0] should be ShowName | [1] should be sXXeXX | [2] should be episode name (if exists)
                string[] fileNameParts = Path.GetFileNameWithoutExtension(FileName).Split(" - ", 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < fileNameParts.Length; i++)
                {
                    switch (i)
                    {
                        case 0:
                        {
                            ShowName = fileNameParts[i];
                            break;
                        }
                        case 1:
                        {
                            int episodeCharIndex = fileNameParts[i].IndexOf('e', StringComparison.OrdinalIgnoreCase);

                            string seasonString = fileNameParts[i][..episodeCharIndex];  // sXX
                            string episodeString = fileNameParts[i][episodeCharIndex..]; // eYY

                            SeasonInt = Convert.ToInt32(seasonString.Replace("s", string.Empty, StringComparison.OrdinalIgnoreCase));

                            if (episodeString.Contains('-'))
                            {
                                string[] episodeRange = episodeString.Replace("e", string.Empty, StringComparison.OrdinalIgnoreCase).Split('-', 2, StringSplitOptions.TrimEntries);
                                int minEpisode = Convert.ToInt32(episodeRange[0]);
                                int maxEpisode = Convert.ToInt32(episodeRange[1]);
                                EpisodeInts = Enumerable.Range(minEpisode, (maxEpisode - minEpisode) + 1).ToList();
                            }
                            else
                            {
                                EpisodeInts = new List<int>() { Convert.ToInt32(episodeString.Replace("e", string.Empty, StringComparison.OrdinalIgnoreCase)) };
                            }

                            break;
                        }
                        case 2:
                        {
                            EpisodeName = fileNameParts[i];
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                // If an exception is thrown, tack on a message about what file and rethrow
                throw new Exception($"Issue with creating {nameof(ShowSourceFileData)} for {sourceFileData.FullPath}", ex);
            }
        }
    }
}
