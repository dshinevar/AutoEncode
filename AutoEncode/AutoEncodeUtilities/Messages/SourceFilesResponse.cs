using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Messages
{
    public class SourceFilesResponse
    {
        public IDictionary<string, IEnumerable<SourceFileData>> MovieSourceFiles { get; set; }

        public IDictionary<string, IEnumerable<ShowSourceFileData>> ShowSourceFiles { get; set; }
    }
}
