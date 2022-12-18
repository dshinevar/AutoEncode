using System;
using System.Collections.Generic;
using System.Text;

namespace AutoEncodeUtilities.Data
{
    public class ClientConnectData
    {
        public Dictionary<string, List<VideoSourceData>> VideoSourceFiles { get; set; }

        public Dictionary<string, List<ShowSourceData>> ShowSourceFiles { get; set; }
    }
}
