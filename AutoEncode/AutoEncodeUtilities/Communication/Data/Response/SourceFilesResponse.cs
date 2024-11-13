using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Communication.Data.Response;

public class SourceFilesResponse
{
    public Dictionary<string, IEnumerable<SourceFileData>> SourceFiles { get; set; }
}
