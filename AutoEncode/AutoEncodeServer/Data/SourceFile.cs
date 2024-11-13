using AutoEncodeUtilities.Enums;
using System.IO;

namespace AutoEncodeServer.Data;

/// <summary>Most base source file data.</summary>
public class SourceFile
{
    public string Filename => Path.GetFileName(FullPath);
    public string FullPath { get; set; }
    public string DestinationFullPath { get; set; }
    public SourceFileEncodingStatus EncodingStatus { get; set; }
    public string SearchDirectoryName { get; set; }
    public string SourceDirectory { get; set; }
    public bool IsEpisode { get; set; }
}
