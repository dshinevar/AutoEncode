using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using System;

namespace AutoEncodeUtilities.Data;

/// <summary>Basic data for a source file -- used for client/server communication.</summary>
public class SourceFileData :
    IUpdateable<SourceFileData>,
    IEquatable<SourceFileData>
{
    #region Properties
    public Guid Guid { get; set; }
    public string Filename { get; set; }
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

    public SourceFileData() { }

    #region Methods
    public bool Equals(SourceFileData data) => Guid.Equals(data.Guid);
    public override bool Equals(object obj)
    {
        if (obj is SourceFileData sourceFileData)
        {
            return Equals(sourceFileData);
        }

        return false;
    }
    public override int GetHashCode() => Guid.GetHashCode();
    public void Update(SourceFileData data) => data.CopyProperties(this);
    public static int CompareByFileName(SourceFileData data1, SourceFileData data2) => string.Compare(data1.Filename, data2.Filename);
    #endregion Methods
}
