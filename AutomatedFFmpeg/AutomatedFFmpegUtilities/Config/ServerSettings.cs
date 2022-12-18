using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedFFmpegUtilities.Config
{
    public class ServerSettings
    {
        public string IP { get; set; }

        public int Port { get; set; }

        public string FFmpegDirectory { get; set; } = string.Empty;

        public string X265FullPath { get; set; } = string.Empty;

        public string HDR10PlusExtractorFullPath { get; set; } = string.Empty;

        public string DolbyVisionExtractorFullPath { get; set; } = string.Empty;

        public string MkvMergeFullPath { get; set; } = string.Empty;

        public LoggerSettings LoggerSettings { get; set; }
    }

    public class LoggerSettings
    {
        public string LogFileLocation { get; set; }

        public long MaxFileSizeInBytes { get; set; }

        public int BackupFileCount { get; set; }
    }
}
