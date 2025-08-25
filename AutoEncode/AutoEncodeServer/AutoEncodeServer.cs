using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Logger;
using Castle.Windsor;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoEncodeServer;

internal partial class AutoEncodeServer
{
    private enum StartupStep
    {
        Startup = 0,
        ContainerLoad = -1,
        ConfigLoad = -2,
        LoggerInitialize = -3,
        FfmpegCheck = -4,
        Hdr10PlusCheck = -5,
        DolbyVisionCheck = -6,
        UnfinishedJobCheck = -7,
        ServerRun = -8
    }

    private const string LOG_FILENAME = "aeserver.log";

    static async Task Main()
    {
        const string LOG_STARTUP = "STARTUP";
        StartupStep startupStep = StartupStep.Startup;
        /// <summary>Helper Method to create startup log name</summary>
        string GetStartupLogName()
            => $"{LOG_STARTUP}-{startupStep}";

        ILogger logger = null;
        Task loggerTask = null;
        IAutoEncodeServerManager serverManager = null;

        // Container Standup
        startupStep = StartupStep.ContainerLoad;
        WindsorContainer container = new();
        RegisterContainerComponents(container);

        ManualResetEvent shutdownMRE = new(false);
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnApplicationExit(serverManager, logger, shutdownMRE);

        HelperMethods.DebugLog("AutoEncodeServer Starting Up.", GetStartupLogName());
        // LOAD CONFIG FILE
        try
        {
            startupStep = StartupStep.ConfigLoad;
            using StreamReader reader = new(Lookups.ConfigFileLocation);
            string str = reader.ReadToEnd();
            var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();

            ServerConfig serverConfig = deserializer.Deserialize<ServerConfig>(str);
            State.LoadFromConfig(serverConfig); // Load config into static State class -- makes available everywhere
        }
        catch (Exception ex)
        {
            HelperMethods.DebugLog($"Error loading config from {Lookups.ConfigFileLocation}{Environment.NewLine}{ex.Message}", GetStartupLogName());
            Environment.Exit((int)startupStep);
        }

        HelperMethods.DebugLog("Config file loaded.", GetStartupLogName());

        // LOGGER STARTUP
        startupStep = StartupStep.LoggerInitialize;
        logger = container.Resolve<ILogger>();

        string logFileDirectory = State.LoggerSettings.LogFileDirectory;
        if (logger.Initialize(logFileDirectory, LOG_FILENAME, State.LoggerSettings.MaxFileSizeInBytes, State.LoggerSettings.BackupFileCount) is false)
        {
            // If initialization failed for logger, attempt backup logger location
            if (logger.Initialize(Lookups.LogBackupFileLocation, LOG_FILENAME, State.LoggerSettings.MaxFileSizeInBytes, State.LoggerSettings.BackupFileCount) is false)
            {
                HelperMethods.DebugLog("Failed to initialize logger. Shutting down.", GetStartupLogName());
                Environment.Exit((int)startupStep);
            }
        }

        loggerTask = logger.Run();

        // CHECK FOR FFMPEG/FFPROBE
        startupStep = StartupStep.FfmpegCheck;
        int ffmpegAndFfprobeIndent = "FFmpeg: ".Length;
        List<string> ffmpegMessages = [];
        try
        {
            List<string> ffmpegVersion = CheckForFfmpeg(State.Ffmpeg.FfmpegDirectory);
            if ((ffmpegVersion?.Count ?? 0) == 0)
                throw new Exception("ffmpeg not found or unable to determine version.");

            if (ffmpegVersion.Last().Contains("Exiting with exit code"))
                ffmpegVersion.RemoveAt(ffmpegVersion.Count - 1);    // Remove exit message

            ffmpegMessages.Add("FFmpeg:");
            for (int i = 0; i < ffmpegVersion.Count; i++)
            {
                ffmpegMessages.Add(ffmpegVersion[i].Indent(ffmpegAndFfprobeIndent));
            }
        }
        catch (Exception ex)
        {
            HelperMethods.DebugLog($"FATAL: ffmpeg not found/failed to call. Exiting. Exception: {ex.Message}", GetStartupLogName());
            logger.LogException(ex, "ffmpeg not found/failed to call. Exiting.", GetStartupLogName(), details: new { State.Ffmpeg.FfmpegDirectory });
            Environment.Exit((int)startupStep);
        }

        List<string> ffprobeMessages = [];
        try
        {
            // If not provided a specific ffprobe directory, use the ffmpeg directory
            // The CheckFor method will handle if that is also null/empty
            string ffprobeDirectory = State.Ffmpeg.FfprobeDirectory;
            if (string.IsNullOrWhiteSpace(ffprobeDirectory))
            {
                ffprobeDirectory = State.Ffmpeg.FfprobeDirectory;
            }

            List<string> ffprobeVersion = CheckForFfprobe(ffprobeDirectory);
            if ((ffprobeVersion?.Count ?? 0) == 0)
                throw new Exception("ffprobe not found or unable to determine version.");

            if (ffprobeVersion.Last().Contains("Exiting with exit code"))
                ffprobeVersion.RemoveAt(ffprobeVersion.Count - 1);    // Remove exit message

            ffprobeMessages.Add("FFprobe:");
            for (int i = 0; i < ffprobeVersion.Count; i++)
            {
                ffprobeMessages.Add(ffprobeVersion[i].Indent(ffmpegAndFfprobeIndent));
            }
        }
        catch (Exception ex)
        {
            HelperMethods.DebugLog($"FATAL: ffprobe not found/failed to call. Exiting. Exception: {ex.Message}", GetStartupLogName());
            logger.LogException(ex, "ffprobe not found/failed to call. Exiting.", GetStartupLogName(), details: new { State.Ffmpeg.FfprobeDirectory, State.Ffmpeg.FfmpegDirectory });
            Environment.Exit((int)startupStep);
        }

        // HDR10PLUS CHECK
        // If disabled by config, don't do anything else
        startupStep = StartupStep.Hdr10PlusCheck;
        List<string> hdr10PlusMessages = [];
        if (State.Hdr10Plus.Enabled is true)
        {
            int indent = "HDR10+: ".Length;
            try
            {

                string hdr10PlusToolVersion = CheckForHdr10PlusTool(State.Hdr10Plus.Hdr10PlusToolFullPath);
                if (string.IsNullOrWhiteSpace(hdr10PlusToolVersion))
                {
                    // Failed to find hdr10plus_tool, just disable hdr10plus encoding
                    State.DisableHdr10Plus();
                    hdr10PlusMessages.Add("HDR10+: Disabled");
                    hdr10PlusMessages.Add("Failed to find hdr10plus_tool.".Indent(indent));
                    hdr10PlusMessages.Add($"Path (Not Found): {State.Hdr10Plus.Hdr10PlusToolFullPath}".Indent(indent));
                }
                else
                {
                    hdr10PlusMessages.Add("HDR10+: Enabled");
                    hdr10PlusMessages.Add($"Path (Found): {State.Hdr10Plus.Hdr10PlusToolFullPath}".Indent(indent));
                    hdr10PlusMessages.Add(hdr10PlusToolVersion.Indent(indent));
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex, "Exception occurred while checking for hdr10plus_tool. Disabling HDR10+ encoding.", GetStartupLogName(), new { State.Hdr10Plus.Hdr10PlusToolFullPath });
                State.DisableHdr10Plus();
                hdr10PlusMessages.Add("HDR10+: Disabled");
                hdr10PlusMessages.Add("Exception occurred with hdr10plus_tool. Look for log message.".Indent(indent));
                hdr10PlusMessages.Add($"Path: {State.Hdr10Plus.Hdr10PlusToolFullPath}".Indent(indent));
            }
        }
        else
            hdr10PlusMessages.Add("HDR10+: Disabled");


        // DOLBY VISION CHECK: CHECK FOR dovi_tool, mkvmerge, AND x265
        startupStep = StartupStep.DolbyVisionCheck;
        int dolbyVisionIndent = "DolbyVision: ".Length;
        int dolbyVisionSecondaryIndent = dolbyVisionIndent + 6;
        // If disabled by config, don't do anything else
        // dovi_tool check
        List<string> doviToolMessages = [];
        if (State.DolbyVision.Enabled is true)
        {
            doviToolMessages.Add("dovi_tool:".Indent(dolbyVisionIndent)); ;
            try
            {
                string doviToolVersion = CheckForDoviTool(State.DolbyVision.DoviToolFullPath);
                if (string.IsNullOrWhiteSpace(doviToolVersion))
                {
                    // Failed to find dovi_tool, just disable dolby vision encoding
                    State.DisableDolbyVision();
                    doviToolMessages.Add("Failed to find dovi_tool.".Indent(dolbyVisionSecondaryIndent));
                    doviToolMessages.Add($"Path (Not Found) {State.DolbyVision.DoviToolFullPath}".Indent(dolbyVisionSecondaryIndent));
                }
                else
                {
                    doviToolMessages.Add($"Path (Found): {State.DolbyVision.DoviToolFullPath}".Indent(dolbyVisionSecondaryIndent));
                    doviToolMessages.Add(doviToolVersion.Indent(dolbyVisionSecondaryIndent));
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex, "Exception occurred while checking for dovi_tool. Disabling DolbyVision encoding.", GetStartupLogName(), new { State.DolbyVision.DoviToolFullPath });
                State.DisableDolbyVision();
                doviToolMessages.Add("Exception occurred with dovi_tool. Look for log message.".Indent(dolbyVisionSecondaryIndent));
                doviToolMessages.Add($"Path: {State.DolbyVision.DoviToolFullPath}".Indent(dolbyVisionSecondaryIndent));
            }
        }

        // Ensure dolby vision wasn't disabled from above
        // x265 check
        List<string> x265Messages = [];
        if (State.DolbyVision.Enabled is true)
        {
            x265Messages.Add("x265:".Indent(dolbyVisionIndent));
            try
            {
                List<string> x265Version = CheckForX265(State.DolbyVision.X265FullPath);
                if ((x265Version?.Count ?? 0) == 0)
                {
                    // Failed to find x265, disable dolby vision encoding
                    State.DisableDolbyVision();
                    x265Messages.Add("Failed to find x265.".Indent(dolbyVisionSecondaryIndent));
                    x265Messages.Add($"Path (Not Found) {State.DolbyVision.X265FullPath}".Indent(dolbyVisionSecondaryIndent));
                }
                else
                {
                    x265Messages.Add($"Path (Found) {State.DolbyVision.X265FullPath}".Indent(dolbyVisionSecondaryIndent));
                    for (int i = 0; i < x265Version.Count; i++)
                    {
                        x265Messages.Add(x265Version[i].Indent(dolbyVisionSecondaryIndent));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex, "Exception occurred while checking for x265. Disabling DolbyVision encoding.", GetStartupLogName(), new { State.DolbyVision.X265FullPath });
                State.DisableDolbyVision();
                x265Messages.Add("Exception occurred with x265. Look for log message.".Indent(dolbyVisionSecondaryIndent));
                x265Messages.Add($"Path: {State.DolbyVision.X265FullPath}".Indent(dolbyVisionSecondaryIndent));
            }
        }

        // Ensure dolby vision wasn't disabled from above
        // mkvmerge check
        List<string> mkvMergeMessages = [];
        if (State.DolbyVision.Enabled is true)
        {
            mkvMergeMessages.Add("mkvmerge:".Indent(dolbyVisionIndent));
            try
            {
                string mkvMergeVersion = CheckForMkvMerge(State.DolbyVision.MkvMergeFullPath);
                if (string.IsNullOrWhiteSpace(mkvMergeVersion))
                {
                    // Failed to find mkvmerge, disable dolby vision encoding
                    State.DisableDolbyVision();
                    mkvMergeMessages.Add("Failed to find mkvmerge.".Indent(dolbyVisionSecondaryIndent));
                    mkvMergeMessages.Add($"Path (Not Found) {State.DolbyVision.MkvMergeFullPath}".Indent(dolbyVisionSecondaryIndent));
                }
                else
                {
                    mkvMergeMessages.Add($"Path (Found): {State.DolbyVision.MkvMergeFullPath}".Indent(dolbyVisionSecondaryIndent));
                    mkvMergeMessages.Add(mkvMergeVersion.Indent(dolbyVisionSecondaryIndent));
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex, "Exception occurred while checking for mkvmerge. Disabling DolbyVision encoding.", GetStartupLogName(), new { State.DolbyVision.MkvMergeFullPath });
                State.DisableDolbyVision();
                mkvMergeMessages.Add("Exception occurred with mkvmerge. Look for log message.".Indent(dolbyVisionSecondaryIndent));
                mkvMergeMessages.Add($"Path: {State.DolbyVision.MkvMergeFullPath}".Indent(dolbyVisionSecondaryIndent));
            }
        }

        // Build DolbyVision startup message
        List<string> dolbyVisionMessages = [$"DolbyVision: {(State.DolbyVision.Enabled ? "Enabled" : "Disabled")}"];
        if (doviToolMessages.Count != 0)
            dolbyVisionMessages.AddRange(doviToolMessages);
        if (x265Messages.Count != 0)
            dolbyVisionMessages.AddRange(x265Messages);
        if (mkvMergeMessages.Count != 0)
            dolbyVisionMessages.AddRange(mkvMergeMessages);

        // GET AND LOG STARTUP AND VERSION
        List<string> startupLog = [];

        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        startupLog.Add($"AutoEncodeServer V{version} Starting Up. Config file loaded.");
        startupLog.Add($"CLIENT UPDATE PORT|COMM PORT: {State.ConnectionSettings.ClientUpdatePort}|{State.ConnectionSettings.CommunicationPort}");
        startupLog.Add($"SUPPORTED FILE EXTENSIONS: {string.Join(", ", State.VideoFileExtensions)}");
        if (!string.IsNullOrWhiteSpace(State.SecondarySkipExtension))
            startupLog.Add($"SKIP SECONDARY EXTENSION: {State.SecondarySkipExtension}");
        startupLog.AddRange(ffmpegMessages);
        startupLog.AddRange(ffprobeMessages);
        startupLog.AddRange(hdr10PlusMessages);
        startupLog.AddRange(dolbyVisionMessages);

        logger.LogInfo(startupLog, LOG_STARTUP);

        // CHECK FOR TEMP FILE OF UNFINISHED ENCODING JOB AND DELETE
        startupStep = StartupStep.UnfinishedJobCheck;
        try
        {
            if (File.Exists(Lookups.PreviouslyEncodingTempFile))
            {
                IEnumerable<string> filesToDelete = File.ReadLines(Lookups.PreviouslyEncodingTempFile);

                foreach (string file in filesToDelete)
                {
                    string fileToDelete = file.Trim();
                    if (File.Exists(fileToDelete))
                    {
                        File.Delete(fileToDelete);
                        logger.LogInfo($"Deleted previously unfinished encoding job file (most likely from unexpected shutdown): {fileToDelete}", GetStartupLogName());
                    }
                }

                File.Delete(Lookups.PreviouslyEncodingTempFile);
            }
        }
        catch (Exception ex)
        {
            // Can continue if an error occurs here
            Debug.WriteLine($"Failed to delete previously encoding file or temp file. Exception: {ex.Message}");
            logger.LogException(ex, "Failed to delete previously encoding file or temp file.", GetStartupLogName());
        }

        // SERVER STARTUP
        startupStep = StartupStep.ServerRun;
        try
        {
            serverManager = container.Resolve<IAutoEncodeServerManager>();
            serverManager.Initialize();
            await serverManager.Run();  // Runs the server -- OnApplicationExit should initiate shutdown gracefully
        }
        catch (Exception ex)
        {
            logger?.LogException(ex, "Exception while running AutoEncodeServer", nameof(AutoEncodeServer));

            NetMQConfig.Cleanup();

            logger.Stop();
            loggerTask.Wait(10000);

            container.Release(serverManager);
            serverManager = null;

            Environment.Exit((int)startupStep);
        }

        // Final cleanup
        NetMQConfig.Cleanup();

        logger.Stop();
        loggerTask.Wait(10000);

        container.Release(serverManager);
        serverManager = null;

        ContainerCleanup(container);

        shutdownMRE.Set();  // Lets OnApplicationExit complete -- helps ensure there isn't premature shutdown
    }

    static void OnApplicationExit(IAutoEncodeServerManager autoEncodeServerManager, ILogger logger, ManualResetEvent shutdownMRE)
    {
        logger?.LogInfo("AutoEncodeServer Shutdown Initiated.", "SHUTDOWN");
        autoEncodeServerManager?.Shutdown();
        shutdownMRE.WaitOne();  // Hold here to allow for the server to completely shutdown -- if this exits, the app will exit
    }
}
