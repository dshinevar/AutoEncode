using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Config;

public class ServerConfig
{
    public int MaxNumberOfJobsInQueue { get; set; } = 50;

    public int HoursCompletedUntilRemoval { get; set; } = 1;

    public int HoursErroredUntilRemoval { get; set; } = 2;

    public string[] VideoFileExtensions { get; set; } = [".mkv", ".m4v", ".avi"];

    public string SecondarySkipExtension { get; set; } = "skip";

    public FfmpegSettings Ffmpeg { get; set; }

    public Hdr10PlusSettings Hdr10Plus { get; set; }

    public DolbyVisionSettings DolbyVision { get; set; }

    public LoggerSettings Logger { get; set; }

    public ServerConnectionSettings Connection { get; set; }

    public Dictionary<string, SearchDirectory> Directories { get; set; }
}
