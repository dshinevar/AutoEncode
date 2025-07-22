using AutoEncodeUtilities.Enums;
using System;

namespace AutoEncodeUtilities.Data;

/// <summary>Basic data for a source file -- used for client/server communication.</summary>
public class SourceFileData
{
    #region Properties
    public Guid Guid { get; set; }
    public string FileName { get; set; }
    public string FileNameWithoutExtension { get; set; }
    public string FullPath { get; set; }
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
}
