using AutoEncodeServer.Data;
using AutoEncodeServer.Models.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.IO;
using System.Linq;

namespace AutoEncodeServer.Models;

public class SourceFileModel : ISourceFileModel
{
    #region Properties
    public Guid Guid { get; set; }
    public string FullPath { get; set; }
    public string Filename { get; set; }
    public string DestinationFullPath { get; set; }
    public SourceFileEncodingStatus EncodingStatus { get; set; }

    #region Search Directory Properties
    public string SearchDirectoryName { get; set; }
    public string SourceDirectory { get; set; }
    #endregion Search Directory Properties

    #region Show Specific Properties
    public bool IsEpisode { get; set; } = false;
    public string ShowName { get; set; }
    public string Season { get; set; }
    public byte SeasonNumber { get; set; }
    public string EpisodeName { get; set; }
    public int[] EpisodeNumbers { get; set; }   // Could span multiple episodes
    #endregion Show Specific Properties

    #endregion Properties

    public SourceFileModel(SourceFile sourceFile)
    {
        Guid = Guid.NewGuid();
        sourceFile.CopyProperties(this);

        if (IsEpisode is true)
        {
            DetermineEpisodeInfo();
        }
    }

    #region Public Methods
    public SourceFileData ToData()
    {
        SourceFileData sourceFileData = new();
        this.CopyProperties(sourceFileData);
        return sourceFileData;
    }

    public bool UpdateEncodingStatus(SourceFileEncodingStatus encodingStatus)
    {
        if (encodingStatus != EncodingStatus)
        {
            EncodingStatus = encodingStatus;
            return true;
        }

        return false;
    }
    #endregion Public Methods

    #region Private Methods
    private void DetermineEpisodeInfo()
    {
        // Assumes file naming of: ShowName - s00e00 - EpisodeName
        try
        {
            // [0] should be ShowName | [1] should be sXXeXX | [2] should be episode name (if exists)
            string[] fileNameParts = Path.GetFileNameWithoutExtension(Filename).Split(" - ", 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
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

                        if (byte.TryParse(seasonString.Replace("s", string.Empty, StringComparison.OrdinalIgnoreCase), out byte seasonNumber))
                        {
                            SeasonNumber = seasonNumber;
                        }

                        if (episodeString.Contains('-'))
                        {
                            string[] episodeRange = episodeString.Replace("e", string.Empty, StringComparison.OrdinalIgnoreCase).Split('-', 2, StringSplitOptions.TrimEntries);
                            int minEpisode = Convert.ToInt32(episodeRange[0]);
                            int maxEpisode = Convert.ToInt32(episodeRange[1]);
                            EpisodeNumbers = Enumerable.Range(minEpisode, (maxEpisode - minEpisode) + 1).ToArray();
                        }
                        else
                        {
                            EpisodeNumbers = [Convert.ToInt32(episodeString.Replace("e", string.Empty, StringComparison.OrdinalIgnoreCase))];
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
            // Determining episode info is "extra" -- don't throw errors for now.
            HelperMethods.DebugLog($"Issue with determining episode info for {FullPath} -- filename may be in an incorrect format. {ex.Message}", nameof(SourceFileModel));
        }
    }
    #endregion Private Methods
}
