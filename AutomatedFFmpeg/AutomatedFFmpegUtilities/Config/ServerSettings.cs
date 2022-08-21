using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedFFmpegUtilities.Config
{
    public class ServerSettings
    {
        public string IP { get; set; }

        public int Port { get; set; }

        public string[] VideoFileExtensions { get; set; } = new[] { ".mkv", ".m4v", ".avi" };

        public int ThreadSleepInMS { get; set; } = 300000; // 5 minutes

        public string FFmpegDirectory { get; set; }

        public string HDR10PlusExtractorFullPath { get; set; }

        public string DolbyVisionExtractorFullPath { get; set; }

        public LoggerSettings LoggerSettings { get; set; }
    }

    public class LoggerSettings
    {
        public string LogFileLocation { get; set; }

        public long MaxFileSizeInBytes { get; set; }

        public int BackupFileCount { get; set; }
    }
}
