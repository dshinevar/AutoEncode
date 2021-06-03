using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedFFmpegUtilities.Config
{
    public class ServerSettings
    {
        public string IP { get; set; }

        public int Port { get; set; }

        public string[] VideoFileExtensions { get; set; } = new[] { ".mkv", ".m4v" };
    }
}
