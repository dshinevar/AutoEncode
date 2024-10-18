namespace AutoEncodeUtilities.Interfaces;

/// <summary>Defines common source file data </summary>
public interface ISourceFileData
{
    string FileName { get; }

    string FullPath { get; }

    string DestinationFullPath { get; }

    bool Encoded { get; }
}
