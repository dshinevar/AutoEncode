using AutoEncodeUtilities.Enums;

namespace AutoEncodeUtilities.Interfaces;

/// <summary>Defines common source file data </summary>
public interface ISourceFileData
{
    string FileName { get; }

    string FullPath { get; }

    string DestinationFullPath { get; }

    SourceFileEncodingStatus EncodingStatus { get; }
}
