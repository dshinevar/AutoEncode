using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Messages;

public class SourceFilesResponse
{
    public IDictionary<string, (bool IsShows, IEnumerable<SourceFileData> Files)> SourceFiles { get; set; }
}
