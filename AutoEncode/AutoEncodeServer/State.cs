using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeServer;

public static class State
{
    public static FfmpegSettings Ffmpeg { get; private set; }

    public static Hdr10PlusSettings Hdr10Plus { get; private set; }

    public static DolbyVisionSettings DolbyVision { get; private set; }

    public static int MaxNumberOfJobsInQueue { get; private set; } = 20;

    public static int HoursCompletedUntilRemoval { get; private set; } = 1;

    public static int HoursErroredUntilRemoval { get; private set; } = 2;

    public static string[] VideoFileExtensions { get; private set; } = [".mkv", ".m4v", ".avi"];

    public static string SecondarySkipExtension { get; private set; } = "skip";

    public static LoggerSettings LoggerSettings { get; private set; }

    public static ServerConnectionSettings ConnectionSettings { get; private set; }

    public static Dictionary<string, SearchDirectory> Directories { get; private set; }

    internal static void DisableHdr10Plus() => Hdr10Plus.Enabled = false;
    internal static void EnableHdr10Plus() => Hdr10Plus.Enabled = true;
    internal static void DisableDolbyVision() => DolbyVision.Enabled = false;
    internal static void EnableDolbyVision() => DolbyVision.Enabled = true;

    internal static void LoadFromConfig(ServerConfig config)
    {
        Ffmpeg = config.Ffmpeg ?? new();
        Hdr10Plus = config.Hdr10Plus ?? new();
        DolbyVision = config.DolbyVision ?? new();
        MaxNumberOfJobsInQueue = config.MaxNumberOfJobsInQueue;
        HoursCompletedUntilRemoval = config.HoursCompletedUntilRemoval;
        HoursErroredUntilRemoval = config.HoursErroredUntilRemoval;
        VideoFileExtensions = config.VideoFileExtensions;
        SecondarySkipExtension = config.SecondarySkipExtension;
        LoggerSettings = config.Logger;
        ConnectionSettings = config.Connection;
        Directories = config.Directories;
    }
}
