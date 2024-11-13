using AutoEncodeUtilities.Enums;
using System;

namespace AutoEncodeServer.Data.Request;

/// <summary>Request data object for updating a source file encoding status. </summary>
internal class UpdateSourceFileEncodingStatusRequest
{
    public Guid SourceFileGuid { get; set; }

    public EncodingJobStatus EncodingJobStatus { get; set; }
}
