namespace AutoEncodeServer.Data;

/// <summary>Most base source file data.</summary>
public class SourceFile
{
    /// <summary>FileName of the source file </summary>
    public string FileName { get; init; }
    /// <summary>FileName without extension (should use <see cref="System.IO.Path.GetFileNameWithoutExtension(string?)"/>)</summary>
    public string FileNameWithoutExtension { get; init; }
    /// <summary>Full directory path of the source file.</summary>
    public string FullPath { get; init; }
    /// <summary>Expected destination full path of the source file once encoded.</summary>
    public string DestinationFullPath { get; init; }
    /// <summary>Set if the source file has a matching destination file.</summary>
    public bool HasDestinationFile { get; init; }
    /// <summary>User-defined name of the directory the source file is found in</summary>
    public string SearchDirectoryName { get; init; }
    /// <summary>Directory source file is found in.</summary>
    public string SourceDirectory { get; init; }
    /// <summary>Flag that indicates if the source file is a TV episode or not </summary>
    public bool IsEpisode { get; init; }
}
