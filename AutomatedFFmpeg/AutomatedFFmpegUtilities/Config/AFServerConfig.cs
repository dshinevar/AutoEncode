using AutomatedFFmpegUtilities.Data;
using System.Collections.Generic;

namespace AutomatedFFmpegUtilities.Config
{
    public class AFServerConfig
    {
        public ServerSettings ServerSettings { get; set; }
        public JobFinderSettings JobFinderSettings { get; set; }
        public GlobalJobSettings GlobalJobSettings { get; set; } = new GlobalJobSettings();
        public Dictionary<string, SearchDirectory> Directories { get; set; }
    }
}
