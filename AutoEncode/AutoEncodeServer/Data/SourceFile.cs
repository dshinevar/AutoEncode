using AutoEncodeUtilities.Enums;

namespace AutoEncodeServer.Data;

/// <summary>Most base source file data.</summary>
public class SourceFile
{
    public string Filename { get; init; }
    public string FullPath { get; init; }
    public string DestinationFullPath { get; init; }
    public SourceFileEncodingStatus EncodingStatus { get; init; }
    public string SearchDirectoryName { get; init; }
    public string SourceDirectory { get; init; }
    public bool IsEpisode { get; init; }
}
