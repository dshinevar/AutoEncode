using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Config;

public class ServerConfig
{
    public string FFmpegDirectory { get; set; } = string.Empty;

    public string X265FullPath { get; set; } = string.Empty;

    public string HDR10PlusExtractorFullPath { get; set; } = string.Empty;

    public string DolbyVisionExtractorFullPath { get; set; } = string.Empty;

    public bool DolbyVisionEncodingEnabled { get; set; } = true;

    public string MkvMergeFullPath { get; set; } = string.Empty;

    public int MaxNumberOfJobsInQueue { get; set; } = 20;

    public int HoursCompletedUntilRemoval { get; set; } = 1;

    public int HoursErroredUntilRemoval { get; set; } = 2;

    public string[] VideoFileExtensions { get; set; } = [".mkv", ".m4v", ".avi"];

    public string SecondarySkipExtension { get; set; } = "skip";

    public LoggerSettings LoggerSettings { get; set; }

    public ConnectionSettings ConnectionSettings { get; set; }

    public Dictionary<string, SearchDirectory> Directories { get; set; }
}
