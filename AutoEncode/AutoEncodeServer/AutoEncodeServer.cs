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
using System.Reflection;
using System.Threading;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoEncodeServer;

internal partial class AutoEncodeServer
{
    private const string LOG_STARTUP_NAME = "STARTUP";
    private const string LOG_FILENAME = "aeserver.log";

    static void Main()
    {
        // Container Standup
        WindsorContainer container = new();
        RegisterContainerComponents(container);

        ILogger logger = null;
        IAutoEncodeServerManager serverManager = null;
        ManualResetEvent shutdownMRE = new(false);

        AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnApplicationExit(sender, e, serverManager, shutdownMRE, container, logger);

        Debug.WriteLine("AutoEncodeServer Starting Up.");
        // LOAD CONFIG FILE
        try
        {
            using StreamReader reader = new(Lookups.ConfigFileLocation);
            string str = reader.ReadToEnd();
            var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();

            ServerConfig serverConfig = deserializer.Deserialize<ServerConfig>(str);
            State.LoadFromConfig(serverConfig); // Load config into static State class -- makes available everywhere
        }
        catch (Exception ex)
        {
            HelperMethods.DebugLog($"Error loading config from {Lookups.ConfigFileLocation}{Environment.NewLine}{ex.Message}", LOG_STARTUP_NAME);
            Environment.Exit(-2);
        }

        HelperMethods.DebugLog("Config file loaded.", LOG_STARTUP_NAME);

        // LOGGER STARTUP
        logger = container.Resolve<ILogger>();

        string logFileDirectory = State.LoggerSettings.LogFileDirectory;
        if (logger.Initialize(logFileDirectory, LOG_FILENAME, State.LoggerSettings.MaxFileSizeInBytes, State.LoggerSettings.BackupFileCount) is false)
        {
            // If initialization failed for logger, attempt backup logger location
            if (logger.Initialize(Lookups.LogBackupFileLocation, LOG_FILENAME, State.LoggerSettings.MaxFileSizeInBytes, State.LoggerSettings.BackupFileCount) is false)
            {
                HelperMethods.DebugLog("Failed to initialize logger. Shutting down.", LOG_STARTUP_NAME);
                Environment.Exit(-2);
            }
        }

        if (logger.CheckAndDoRollover() is false)
        {
            HelperMethods.DebugLog("FATAL: Error occurred when checking log file for rollover. Exiting as logging will not function.", LOG_STARTUP_NAME);
            Environment.Exit(-2);
        }

        // CHECK FOR FFMPEG
        List<string> ffmpegVersion = null;
        try
        {
            ffmpegVersion = GetFFmpegVersion(State.FFmpegDirectory);
            if (ffmpegVersion?.Count == 0)
            {
                throw new Exception("No ffmpeg version returned.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FATAL: ffmpeg not found/failed to call. Exiting. Exception: {ex.Message}");
            logger.LogException(ex, "ffmpeg not found/failed to call. Exiting.", LOG_STARTUP_NAME, State.FFmpegDirectory);
            Environment.Exit(-2);
        }

        // HDR10PLUS EXTRACTOR CHECK
        bool hdr10PlusExtractorFound = !string.IsNullOrWhiteSpace(State.HDR10PlusExtractorFullPath) && File.Exists(State.HDR10PlusExtractorFullPath);

        // DOLBY VISION: CHECK FOR EXTRACTOR, MKVMERGE, AND X265
        string mkvmergeVersion = string.Empty;
        List<string> x265Version = null;
        bool dolbyVisionExtractorFound = !string.IsNullOrWhiteSpace(State.DolbyVisionExtractorFullPath) && File.Exists(State.DolbyVisionExtractorFullPath);

        bool mkvmergeFound = false;
        if (State.DolbyVisionEncodingEnabled is true && dolbyVisionExtractorFound is true)
        {
            try
            {
                mkvmergeVersion = GetMKVMergeVersion(State.MkvMergeFullPath);
                mkvmergeFound = !string.IsNullOrWhiteSpace(mkvmergeVersion);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to find/call mkvmerge. DolbyVision disabled. Exception: {ex.Message}");
                logger.LogException(ex, "Failed to find/call mkvmerge. DolbyVision disabled.", LOG_STARTUP_NAME, State.MkvMergeFullPath);
            }

            try
            {
                if (string.IsNullOrWhiteSpace(State.X265FullPath) is false)
                {
                    x265Version = Getx265Version(State.X265FullPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to find/call x265. DolbyVision disabled. Exception: {ex.Message}");
                logger.LogException(ex, "Failed to find/call x265. DolbyVision disabled.", LOG_STARTUP_NAME, State.X265FullPath);
            }
        }

        bool dolbyVisionEnabled = State.DolbyVisionEncodingEnabled &&
                                    dolbyVisionExtractorFound is true &&
                                    !string.IsNullOrWhiteSpace(mkvmergeVersion) &&
                                    (x265Version?.Count > 0);

        if (dolbyVisionEnabled is false)
            State.DisableDolbyVision();

        // GET AND LOG STARTUP AND VERSION
        List<string> startupLog = [];

        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        startupLog.Add($"AutoEncodeServer V{version} Starting Up. Config file loaded.");
        startupLog.Add($"IP|CLIENT UPDATE PORT|COMM PORT: {State.ConnectionSettings.IPAddress}|{State.ConnectionSettings.ClientUpdatePort}|{State.ConnectionSettings.CommunicationPort}");
        startupLog.Add($"SUPPORTED FILE EXTENSIONS: {string.Join(", ", State.VideoFileExtensions)}");
        if (!string.IsNullOrWhiteSpace(State.SecondarySkipExtension)) startupLog.Add($"SKIP SECONDARY EXTENSION: {State.SecondarySkipExtension}");
        startupLog.Add($"DOLBY VISION: {(dolbyVisionEnabled ? "ENABLED" : "DISABLED")}");
        if (dolbyVisionEnabled)
        {
            startupLog.Add($"DOLBY VISION EXTRACTOR: {State.DolbyVisionExtractorFullPath} ({(dolbyVisionExtractorFound ? "FOUND" : "NOT FOUND")})");
            startupLog.Add($"x265: {State.X265FullPath}");
            startupLog.Add($"MKVMERGE: {State.MkvMergeFullPath} ({(mkvmergeFound ? "FOUND" : "NOT FOUND")})");
        }
        if (!string.IsNullOrWhiteSpace(State.HDR10PlusExtractorFullPath))
            startupLog.Add($"HDR10PLUS EXTRACTOR: {State.HDR10PlusExtractorFullPath} ({(hdr10PlusExtractorFound ? "FOUND" : "NOT FOUND")})");
        startupLog.Add($"FFMPEG DIRECTORY: {State.FFmpegDirectory}");
        startupLog.AddRange(ffmpegVersion);
        startupLog.Add(mkvmergeVersion);
        if (dolbyVisionEnabled)
        {
            startupLog.AddRange(x265Version);
        }

        logger.LogInfo(startupLog, LOG_STARTUP_NAME);

        // CHECK FOR TEMP FILE OF UNFINISHED ENCODING JOB AND DELETE
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
                    }
                }

                File.Delete(Lookups.PreviouslyEncodingTempFile);
            }
        }
        catch (Exception ex)
        {
            // Can continue if an error occurs here
            Debug.WriteLine($"Failed to delete previously encoding file or temp file. Exception: {ex.Message}");
            logger.LogException(ex, "Failed to delete previously encoding file or temp file.", LOG_STARTUP_NAME);
        }

        // SERVER STARTUP
        try
        {
            serverManager = container.Resolve<IAutoEncodeServerManager>();
            serverManager.Initialize(shutdownMRE);
            serverManager.Start();
        }
        catch (Exception ex)
        {
            logger?.LogException(ex, "Failed to Start AutoEncodeServer", LOG_STARTUP_NAME);

            container.Release(serverManager);
            serverManager = null;
            Environment.Exit(-2);
        }

        shutdownMRE.WaitOne();

        container.Release(serverManager);
        serverManager = null;
    }

    static void OnApplicationExit(object sender, EventArgs e, IAutoEncodeServerManager mainThread, ManualResetEvent shutdownMRE, WindsorContainer container, ILogger logger)
    {
        logger?.LogInfo("AutoEncodeServer Shutting Down.", "SHUTDOWN");

        if (mainThread is not null)
        {
            mainThread.Shutdown();
            shutdownMRE.WaitOne();
        }

        NetMQConfig.Cleanup();

        ContainerCleanup(container);
    }
}
