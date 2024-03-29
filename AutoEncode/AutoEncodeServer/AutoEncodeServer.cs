﻿using AutoEncodeServer.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Logger;
using Castle.Windsor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoEncodeServer
{
    internal partial class AutoEncodeServer
    {
        private const string LOG_THREAD_NAME = "STARTUP";
        private const string LOG_FILENAME = "aeserver.log";

        static void Main(string[] args)
        {
            // Container Standup
            WindsorContainer container = new();
            RegisterContainerComponents(container);

            ILogger logger = null;
            IAEServerMainThread mainThread = null;
            AEServerConfig serverConfig = null; // As loaded from file
            AEServerConfig serverState = null; // State after startup checks
            ManualResetEvent Shutdown = new(false);
            List<string> startupLog = [];

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnApplicationExit(sender, e, mainThread, Shutdown, container, logger);

            Debug.WriteLine("AutoEncodeServer Starting Up.");
            // LOAD CONFIG FILE
            try
            {
                using StreamReader reader = new(Lookups.ConfigFileLocation);
                string str = reader.ReadToEnd();
                var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();

                serverConfig = deserializer.Deserialize<AEServerConfig>(str);
                serverState = serverConfig.DeepClone();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Environment.Exit(-2);
            }

            Debug.WriteLine("Config file loaded.");

            // CREATE LOG FILE DIRECTORY
            string LogFileLocation = serverConfig.ServerSettings.LoggerSettings.LogFileLocation;
            try
            {
                CreateLogFileDirectory(LogFileLocation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                if (ex is UnauthorizedAccessException || ex is PathTooLongException)
                {
                    // Exception occurred with given directory, try the backup;  If that fails, exit.
                    try
                    {
                        Directory.CreateDirectory(Lookups.LogBackupFileLocation);
                    }
                    catch (Exception lastChanceEx)
                    {
                        Debug.WriteLine(lastChanceEx.ToString());
                        Environment.Exit(-2);
                    }

                    LogFileLocation = Lookups.LogBackupFileLocation;
                }
                else
                {
                    // Exception we don't want to handle, exit.
                    Environment.Exit(-2);
                }
            }

            logger = container.Resolve<ILogger>();
            logger.Initialize(LogFileLocation,
                LOG_FILENAME,
                serverConfig.ServerSettings.LoggerSettings.MaxFileSizeInBytes,
                serverConfig.ServerSettings.LoggerSettings.BackupFileCount);

            //ITestClass testClass = container.Resolve<ITestClass>(); 


            if (logger.CheckAndDoRollover() is false)
            {
                Debug.WriteLine("FATAL: Error occurred when checking log file for rollover. Exiting as logging will not function.");
                Environment.Exit(-2);
            }

            // CHECK FOR FFMPEG
            List<string> ffmpegVersion = null;
            try
            {
                ffmpegVersion = GetFFmpegVersion(serverConfig.ServerSettings.FFmpegDirectory);
                if (ffmpegVersion?.Count == 0)
                {
                    throw new Exception("No ffmpeg version returned.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FATAL: ffmpeg not found/failed to call. Exiting. Exception: {ex.Message}");
                logger.LogException(ex, "ffmpeg not found/failed to call. Exiting.", LOG_THREAD_NAME, serverConfig.ServerSettings.FFmpegDirectory);
                Environment.Exit(-2);
            }

            // HDR10PLUS EXTRACTOR CHECK
            bool hdr10PlusExtractorFound = !string.IsNullOrWhiteSpace(serverConfig.ServerSettings.HDR10PlusExtractorFullPath) && File.Exists(serverConfig.ServerSettings.HDR10PlusExtractorFullPath);

            // DOLBY VISION: CHECK FOR EXTRACTOR, MKVMERGE, AND X265
            string mkvmergeVersion = string.Empty;
            List<string> x265Version = null;
            bool dolbyVisionExtractorFound = !string.IsNullOrWhiteSpace(serverConfig.ServerSettings.DolbyVisionExtractorFullPath) && File.Exists(serverConfig.ServerSettings.DolbyVisionExtractorFullPath);

            bool mkvmergeFound = false;
            if (serverConfig.GlobalJobSettings.DolbyVisionEncodingEnabled is true && dolbyVisionExtractorFound is true)
            {
                try
                {
                    mkvmergeVersion = GetMKVMergeVersion(serverConfig.ServerSettings.MkvMergeFullPath);
                    mkvmergeFound = !string.IsNullOrWhiteSpace(mkvmergeVersion);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to find/call mkvmerge. DolbyVision disabled. Exception: {ex.Message}");
                    logger.LogException(ex, "Failed to find/call mkvmerge. DolbyVision disabled.", LOG_THREAD_NAME, serverConfig.ServerSettings.MkvMergeFullPath);
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(serverConfig.ServerSettings.X265FullPath) is false)
                    {
                        x265Version = Getx265Version(serverConfig.ServerSettings.X265FullPath);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to find/call x265. DolbyVision disabled. Exception: {ex.Message}");
                    logger.LogException(ex, "Failed to find/call x265. DolbyVision disabled.", LOG_THREAD_NAME, serverConfig.ServerSettings.X265FullPath);
                }
            }

            bool dolbyVisionEnabled = serverConfig.GlobalJobSettings.DolbyVisionEncodingEnabled &&
                                        dolbyVisionExtractorFound is true &&
                                        !string.IsNullOrWhiteSpace(mkvmergeVersion) &&
                                        x265Version?.Any() is true;
            serverState.GlobalJobSettings.DolbyVisionEncodingEnabled = dolbyVisionEnabled;

            // GET AND LOG STARTUP AND VERSION
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            startupLog.Add($"AutoEncodeServer V{version} Starting Up. Config file loaded.");
            startupLog.Add($"IP|CLIENT UPDATE PORT|COMM PORT: {serverConfig.ConnectionSettings.IPAddress}|{serverConfig.ConnectionSettings.ClientUpdatePort}|{serverConfig.ConnectionSettings.CommunicationPort}");
            startupLog.Add($"SUPPORTED FILE EXTENSIONS: {string.Join(", ", serverConfig.JobFinderSettings.VideoFileExtensions)}");
            if (!string.IsNullOrWhiteSpace(serverConfig.JobFinderSettings.SecondarySkipExtension)) startupLog.Add($"SKIP SECONDARY EXTENSION: {serverConfig.JobFinderSettings.SecondarySkipExtension}");
            startupLog.Add($"DOLBY VISION: {(dolbyVisionEnabled ? "ENABLED" : "DISABLED")}");
            if (dolbyVisionEnabled)
            {
                startupLog.Add($"DOLBY VISION EXTRACTOR: {serverConfig.ServerSettings.DolbyVisionExtractorFullPath} ({(dolbyVisionExtractorFound ? "FOUND" : "NOT FOUND")})");
                startupLog.Add($"x265: {serverConfig.ServerSettings.X265FullPath}");
                startupLog.Add($"MKVMERGE: {serverConfig.ServerSettings.MkvMergeFullPath} ({(mkvmergeFound ? "FOUND" : "NOT FOUND")})");
            }
            if (!string.IsNullOrWhiteSpace(serverConfig.ServerSettings.HDR10PlusExtractorFullPath))
                startupLog.Add($"HDR10PLUS EXTRACTOR: {serverConfig.ServerSettings.HDR10PlusExtractorFullPath} ({(hdr10PlusExtractorFound ? "FOUND" : "NOT FOUND")})");
            startupLog.Add($"FFMPEG DIRECTORY: {serverConfig.ServerSettings.FFmpegDirectory}");
            startupLog.AddRange(ffmpegVersion);
            startupLog.Add(mkvmergeVersion);
            if (dolbyVisionEnabled)
            {
                startupLog.AddRange(x265Version);
            }

            logger.LogInfo(startupLog, LOG_THREAD_NAME);

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
                logger.LogException(ex, "Failed to delete previously encoding file or temp file.", LOG_THREAD_NAME);
            }

            // SERVER STARTUP
            try
            {
                mainThread = container.Resolve<IAEServerMainThread>();
                mainThread.Initialize(serverState, serverConfig, Shutdown);
                mainThread.Start();
            }
            catch (Exception ex)
            {
                logger?.LogException(ex, "Failed to Start AutoEncodeServer", LOG_THREAD_NAME);

                container.Release(mainThread);
                mainThread = null;
                Environment.Exit(-2);
            }

            Shutdown.WaitOne();

            container.Release(mainThread);
            mainThread = null;
        }

        static void OnApplicationExit(object sender, EventArgs e, IAEServerMainThread mainThread, ManualResetEvent shutdownMRE, WindsorContainer container, ILogger logger)
        {
            logger?.LogInfo("AutoEncodeServer Shutting Down.", "SHUTDOWN");

            if (mainThread is not null)
            {
                mainThread.Shutdown();
                shutdownMRE.WaitOne();
            }

            ContainerCleanup(container);
        }
    }
}
