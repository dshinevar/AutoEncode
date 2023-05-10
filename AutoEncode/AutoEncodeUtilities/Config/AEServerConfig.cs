using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Config
{
    public class AEServerConfig
    {
        public ServerSettings ServerSettings { get; set; }
        public ConnectionSettings ConnectionSettings { get; set; }
        public JobFinderSettings JobFinderSettings { get; set; }
        public GlobalJobSettings GlobalJobSettings { get; set; } = new GlobalJobSettings();
        public Dictionary<string, SearchDirectory> Directories { get; set; }
    }
}
