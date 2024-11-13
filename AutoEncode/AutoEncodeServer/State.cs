using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeServer;

public static class State
{
    public static string FFmpegDirectory { get; private set; } = string.Empty;

    public static string X265FullPath { get; private set; } = string.Empty;

    public static string HDR10PlusExtractorFullPath { get; private set; } = string.Empty;

    public static string DolbyVisionExtractorFullPath { get; private set; } = string.Empty;

    public static bool DolbyVisionEncodingEnabled { get; private set; } = true;

    public static string MkvMergeFullPath { get; private set; } = string.Empty;

    public static int MaxNumberOfJobsInQueue { get; private set; } = 20;

    public static int HoursCompletedUntilRemoval { get; private set; } = 1;

    public static int HoursErroredUntilRemoval { get; private set; } = 2;

    public static string[] VideoFileExtensions { get; private set; } = [".mkv", ".m4v", ".avi"];

    public static string SecondarySkipExtension { get; private set; } = "skip";

    public static LoggerSettings LoggerSettings { get; private set; }

    public static ConnectionSettings ConnectionSettings { get; private set; }

    public static Dictionary<string, SearchDirectory> Directories { get; private set; }

    internal static void DisableDolbyVision() => DolbyVisionEncodingEnabled = false;

    internal static void LoadFromConfig(ServerConfig config)
    {
        FFmpegDirectory = config.FFmpegDirectory;
        X265FullPath = config.X265FullPath;
        HDR10PlusExtractorFullPath = config.HDR10PlusExtractorFullPath;
        DolbyVisionExtractorFullPath = config.DolbyVisionExtractorFullPath;
        DolbyVisionEncodingEnabled = config.DolbyVisionEncodingEnabled;
        MkvMergeFullPath = config.MkvMergeFullPath;
        MaxNumberOfJobsInQueue = config.MaxNumberOfJobsInQueue;
        HoursCompletedUntilRemoval = config.HoursCompletedUntilRemoval;
        HoursErroredUntilRemoval = config.HoursErroredUntilRemoval;
        VideoFileExtensions = config.VideoFileExtensions;
        SecondarySkipExtension = config.SecondarySkipExtension;
        LoggerSettings = config.LoggerSettings;
        ConnectionSettings = config.ConnectionSettings;
        Directories = config.Directories;
    }
}
