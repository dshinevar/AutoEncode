using AutomatedFFmpegUtilities.Data;
using System.Collections.Generic;

namespace AutomatedFFmpegUtilities.Config
{
    public class AFServerConfig
    {
        public ServerSettings ServerSettings { get; set; }
        public Dictionary<string, SearchDirectory> Directories { get; set; }
    }
}
