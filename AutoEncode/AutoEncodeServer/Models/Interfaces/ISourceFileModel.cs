using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;

namespace AutoEncodeServer.Models.Interfaces;

public interface ISourceFileModel
{
    #region Properties
    Guid Guid { get; }
    string FullPath { get; }
    string FileName { get; }
    string FileNameWithoutExtension { get; }
    string DestinationFullPath { get; }
    SourceFileEncodingStatus EncodingStatus { get; }

    #region Search Directory Properties
    string SearchDirectoryName { get; }
    string SourceDirectory { get; }
    #endregion Search Directory Properties

    #region Show Specific Properties
    bool IsEpisode { get; }
    string ShowName { get; }
    string Season { get; }
    byte SeasonNumber { get; }
    string EpisodeName { get; }
    int[] EpisodeNumbers { get; }   // Could span multiple episodes
    #endregion Show Specific Properties
    #endregion Properties

    SourceFileData ToData();

    /// <summary>Updates the encoding status if it changed. </summary>
    /// <param name="encodingStatus">The updated encoding status.</param>
    /// <returns>True if updated; False, otherwise.</returns>
    bool UpdateEncodingStatus(SourceFileEncodingStatus encodingStatus);
}
