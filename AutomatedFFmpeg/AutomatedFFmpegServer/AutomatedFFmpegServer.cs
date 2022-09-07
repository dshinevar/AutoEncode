using AutomatedFFmpegUtilities;
using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AutomatedFFmpegServer
{
    class AutomatedFFmpegServer
    {
        private const string LOG_THREAD_NAME = "STARTUP";
        private const string LOG_FILENAME = "afserver.log";

        static void Main(string[] args)
        {
            AFServerMainThread mainThread = null;
            AFServerConfig serverConfig = null; // As loaded from file
            AFServerConfig serverState = null; // State after startup checks
            Logger logger = null;
            ManualResetEvent Shutdown = new(false);
            List<string> startupLog = new();

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnApplicationExit(sender, e, mainThread, Shutdown, logger);

            Debug.WriteLine("AutomatedFFmpegServer Starting Up.");
            // LOAD CONFIG FILE
            try
            {
                using StreamReader reader = new(Lookups.ConfigFileLocation);
                string str = reader.ReadToEnd();
                var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();

                serverConfig = deserializer.Deserialize<AFServerConfig>(str);
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
                DirectoryInfo directoryInfo = Directory.CreateDirectory(serverConfig.ServerSettings.LoggerSettings.LogFileLocation);

                if (directoryInfo is null)
                {
                    Debug.WriteLine("Failed to create/find log directory. Checking backup.");

                    DirectoryInfo backupDirectoryInfo = Directory.CreateDirectory(Lookups.LogBackupFileLocation);

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

            // CHECK FOR FFMPEG
            List<string> ffmpegVersion = null;
            try
            {
                ffmpegVersion = GetFFmpegVersion(serverConfig.ServerSettings.FFmpegDirectory);
                if (ffmpegVersion?.Any() is false)
                {
                    throw new Exception("No ffmpeg version returned.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FATAL: ffmpeg not found/failed to call. Exiting. Exception: {ex.Message}");
                logger.LogException(ex, "ffmpeg not found/failed to call. Exiting.", LOG_THREAD_NAME);
                Environment.Exit(-2);
            }

            // DOLBY VISION: CHECK FOR MKVMERGE AND X265
            string mkvmergeVersion = string.Empty;
            List<string> x265Version = null;
            if (serverConfig.GlobalJobSettings.DolbyVisionEncodingEnabled is true)
            {
                try
                {
                    mkvmergeVersion = GetMKVMergeVersion();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to find/call mkvmerge. DolbyVision disabled. Exception: {ex.Message}");
                    logger.LogException(ex, "Failed to find/call mkvmerge. DolbyVision disabled.", LOG_THREAD_NAME);
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
                    logger.LogException(ex, "Failed to find/call x265. DolbyVision disabled.", LOG_THREAD_NAME);
                }
            }

            bool dolbyVisionEnabled = serverConfig.GlobalJobSettings.DolbyVisionEncodingEnabled &&
                                        !string.IsNullOrWhiteSpace(mkvmergeVersion) &&
                                        x265Version?.Any() is true &&
                                        !string.IsNullOrWhiteSpace(serverConfig.ServerSettings.DolbyVisionExtractorFullPath);
            serverState.GlobalJobSettings.DolbyVisionEncodingEnabled = dolbyVisionEnabled;

            // GET AND LOG STARTUP AND VERSION
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            startupLog.Add($"AutomatedFFmpegServer V{version} Starting Up. Config file loaded.");
            startupLog.Add("LOADED CONFIG VALUES");
            startupLog.Add($"IP/PORT: {serverConfig.ServerSettings.IP}:{serverConfig.ServerSettings.Port}");
            startupLog.Add($"SUPPORTED FILE EXTENSIONS: {string.Join(", ", serverConfig.ServerSettings.VideoFileExtensions)}");
            startupLog.Add($"DOLBY VISION: {(dolbyVisionEnabled ? "ENABLED" : "DISABLED")}");

            if (dolbyVisionEnabled)
            {
                startupLog.AddRange(x265Version);
                startupLog.Add($"DOLBY VISION EXTRACTOR: {serverConfig.ServerSettings.DolbyVisionExtractorFullPath}");
            }
            if (!string.IsNullOrWhiteSpace(serverConfig.ServerSettings.HDR10PlusExtractorFullPath)) startupLog.Add($"HDR10PLUS EXTRACTOR: {serverConfig.ServerSettings.HDR10PlusExtractorFullPath}");
            startupLog.Add($"FFMPEG DIRECTORY: {serverConfig.ServerSettings.FFmpegDirectory}");
            startupLog.AddRange(ffmpegVersion);
            startupLog.Add(mkvmergeVersion);

            logger.LogInfo(startupLog, LOG_THREAD_NAME);

            // CHECK FOR TEMP FILE OF UNFINISHED ENCODING JOB AND DELETE
            try
            {
                if (File.Exists(Lookups.PreviouslyEncodingTempFile))
                {
                    string fileToDelete = File.ReadLines(Lookups.PreviouslyEncodingTempFile).First().Trim();

                    if (File.Exists(fileToDelete))
                    {
                        File.Delete(fileToDelete);
                    }

                    File.Delete(Lookups.PreviouslyEncodingTempFile);
                }
            }
            catch (Exception ex)
            {
                // Can continue if an error occurs here
                Debug.WriteLine($"Failed to delete previously encoding file or temp file. Exception: {ex.Message}");
                logger.LogException(ex, "Failed to delete previously encoding file or temp file.", threadName: LOG_THREAD_NAME);
            }

            mainThread = new AFServerMainThread(serverState, serverConfig, logger, Shutdown);
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

        /// <summary>Gets FFmpeg version/Checks to make sure FFmpeg is accessible </summary>
        /// <param name="ffmpegDirectory">FFmpeg directory from config</param>
        /// <returns>List of strings from version output (for logging)</returns>
        static List<string> GetFFmpegVersion(string ffmpegDirectory)
        {
            try
            {
                List<string> ffmpegVersionLines = new();

                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = Path.Combine(ffmpegDirectory, "ffmpeg"),
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

                return ffmpegVersionLines;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>Gets mkvmerge version </summary>
        /// <returns>mkvmerge version string</returns>
        static string GetMKVMergeVersion()
        {
            try
            {
                string mkvMergeVersion = string.Empty;

                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "mkvmerge",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using (Process mkvMergeProcess = new())
                {
                    mkvMergeProcess.StartInfo = startInfo;
                    mkvMergeProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data)) mkvMergeVersion = e.Data; // Only expecting one line
                    };
                    mkvMergeProcess.Start();
                    mkvMergeProcess.BeginOutputReadLine();
                    mkvMergeProcess.WaitForExit();
                }

                return mkvMergeVersion;
            }
            catch (Exception)
            {
                throw;
            }
        }

        static List<string> Getx265Version(string x265FullPath)
        {
            try
            {
                List<string> x265Version = new();

                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = x265FullPath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardError = true
                };

                using (Process x265Process = new())
                {
                    x265Process.StartInfo = startInfo;
                    x265Process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data)) x265Version.Add(e.Data.Replace("x265 [info]: ", string.Empty)); // Only expecting one line
                    };
                    x265Process.Start();
                    x265Process.BeginOutputReadLine();
                    x265Process.WaitForExit();
                }

                return x265Version;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
