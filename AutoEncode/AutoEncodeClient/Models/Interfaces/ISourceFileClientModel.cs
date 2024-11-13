using AutoEncodeUtilities.Enums;
using System;
using System.Threading.Tasks;

namespace AutoEncodeClient.Models.Interfaces;

public interface ISourceFileClientModel
{
    Guid Guid { get; }

    string Filename { get; }

    string FullPath { get; }

    string DestinationFullPath { get; }

    SourceFileEncodingStatus EncodingStatus { get; }

    void UpdateEncodingStatus(SourceFileEncodingStatus encodingStatus);

    Task<bool> RequestEncode();
}
