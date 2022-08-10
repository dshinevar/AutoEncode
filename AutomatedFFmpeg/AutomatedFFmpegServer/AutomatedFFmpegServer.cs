using AutomatedFFmpegUtilities;
using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AutomatedFFmpegServer
{
    class AutomatedFFmpegServer
    {
        private const string LOG_THREAD_NAME = "STARTUP";
        private const string LOG_FILENAME = "AFServer.log";

        static void Main(string[] args)
        {
            AFServerMainThread mainThread = null;
            AFServerConfig serverConfig = null;
            Logger logger = null;
            ManualResetEvent Shutdown = new(false);

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnApplicationExit(sender, e, mainThread, Shutdown, logger);

            Debug.WriteLine("AutomatedFFmpegServer Starting Up.");

            try
            {
                using StreamReader reader = new(Lookups.ConfigFileLocation);
                string str = reader.ReadToEnd();
                var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();

                serverConfig = deserializer.Deserialize<AFServerConfig>(str);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Environment.Exit(-2);
            }

            Debug.WriteLine("Config file loaded.");

            string LogFileLocation = serverConfig.ServerSettings.LoggerSettings.LogFileLocation;
            try
            {
                DirectoryInfo directoryInfo = System.IO.Directory.CreateDirectory(serverConfig.ServerSettings.LoggerSettings.LogFileLocation);

                if (directoryInfo is null)
                {
                    Debug.WriteLine("Failed to create/find log directory. Checking backup.");

                    DirectoryInfo backupDirectoryInfo = System.IO.Directory.CreateDirectory(Lookups.LogBackupFileLocation);

                    if (backupDirectoryInfo is null)
                    {
                        Debug.WriteLine("Failed to create/find backup log directory. Exiting.");
                        Environment.Exit(-2);
                    }

                    LogFileLocation = Lookups.LogBackupFileLocation;
                }
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

            logger = new(LogFileLocation,
                LOG_FILENAME,
                serverConfig.ServerSettings.LoggerSettings.MaxFileSizeInBytes,
                serverConfig.ServerSettings.LoggerSettings.BackupFileCount);


            if (logger.CheckAndDoRollover() is false)
            {
                Debug.WriteLine("FATAL: Error occurred when checking log file for rollover. Exiting as logging will not function.");
                Environment.Exit(-2);
            }


            logger.LogInfo("AutomatedFFmpegServer Starting Up. Config file loaded.", LOG_THREAD_NAME);
            List<string> configLog = new()
            {
                "LOADED CONFIG VALUES",
                $"IP/PORT: {serverConfig.ServerSettings.IP}:{serverConfig.ServerSettings.Port}",
                $"SUPPORTED FILE EXTENSIONS: {string.Join(", ", serverConfig.ServerSettings.VideoFileExtensions)}",
                $"FFMPEG DIRECTORY: {serverConfig.ServerSettings.FFmpegDirectory}"
            };
            logger.LogInfo(configLog, LOG_THREAD_NAME);

            try
            {
                List<string> ffmpegVersionLines = new();

                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = $@"{serverConfig.ServerSettings.FFmpegDirectory.RemoveEndingSlashes()}/ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using (Process ffprobeProcess = new())
                {
                    ffprobeProcess.StartInfo = startInfo;
                    ffprobeProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data)) ffmpegVersionLines.Add(e.Data);
                    };
                    ffprobeProcess.Start();
                    ffprobeProcess.BeginOutputReadLine();
                    ffprobeProcess.WaitForExit();
                }

                logger.LogInfo(ffmpegVersionLines, LOG_THREAD_NAME);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FATAL: ffmpeg not found/failed to call. Exiting. Exception: {ex.Message}");
                logger.LogException(ex, "ffmpeg not found/failed to call. Exiting.", threadName: LOG_THREAD_NAME);
                Environment.Exit(-2);
            }

            mainThread = new AFServerMainThread(serverConfig, logger, Shutdown);
            mainThread.Start();

            Shutdown.WaitOne();

            mainThread = null;
        }

        static void OnApplicationExit(object sender, EventArgs e, AFServerMainThread mainThread, ManualResetEvent shutdownMRE, Logger logger)
        {
            logger?.LogInfo("AutomatedFFmpegServer Shutting Down.", "SHUTDOWN");

            if (mainThread is not null)
            {
                mainThread.Shutdown();
                shutdownMRE.WaitOne();
            }
        }
    }
}
