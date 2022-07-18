using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Logger;
using System;
using System.IO;
using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Runtime.InteropServices;

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using AutomatedFFmpegUtilities;
using System.Text;

namespace AutomatedFFmpegServer
{
    class AutomatedFFmpegServer
    {
#if DEBUG
        private static string ConfigFileLocation = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) 
                                                    ? "./bin/Debug/net6.0/AFServerConfig.yaml"
                                                    : "AFServerConfig.yaml";                                            
#else
        private static string ConfigFileLocation = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) 
                                                    ? "/usr/local/bin/AFServerConfig.yaml"
                                                    : "AFServerConfig.yaml";        
#endif
        private const string LOG_THREAD_NAME = "STARTUP";

        static void Main(string[] args)
        {
            AFServerMainThread mainThread = null;
            AFServerConfig serverConfig = null;
            Logger logger = null;
            ManualResetEvent Shutdown = new ManualResetEvent(false);

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnApplicationExit(sender, e, mainThread, Shutdown, logger);

            Debug.WriteLine("AutomatedFFmpegServer Starting Up.");

            try
            {
                using (var reader = new StreamReader(ConfigFileLocation))
                {
                    string str = reader.ReadToEnd();
                    var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();

                    serverConfig = deserializer.Deserialize<AFServerConfig>(str);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Environment.Exit(-2);
            }

            Debug.WriteLine("Config file loaded.");

            logger = new(serverConfig.ServerSettings.LoggerSettings.LogFileLocation,
                serverConfig.ServerSettings.LoggerSettings.MaxFileSizeInBytes,
                serverConfig.ServerSettings.LoggerSettings.BackupFileCount);

            /*
            if (logger.CheckAndDoRollover() is false)
            {
                Debug.WriteLine("FATAL: Error occurred when checking log file for rollover. Exiting as logging will not function.");
                Environment.Exit(-2);
            }
            */

            //logger.LogInfo("AutomatedFFmpegServer Starting Up. Config file loaded.", threadName: LOG_THREAD_NAME);

            try
            {
                StringBuilder sbFfmpegVersion = new StringBuilder();

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = $@"{serverConfig.ServerSettings.FFmpegDirectory.RemoveEndingSlashes()}/ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using (Process ffprobeProcess = new Process())
                {
                    ffprobeProcess.StartInfo = startInfo;
                    ffprobeProcess.Start();

                    using (StreamReader reader = ffprobeProcess.StandardOutput)
                    {
                        while (reader.Peek() >= 0)
                        {
                            sbFfmpegVersion.Append(reader.ReadLine());
                        }
                    }

                    ffprobeProcess.WaitForExit();
                }

                Debug.WriteLine(sbFfmpegVersion.ToString());
                //logger.LogInfo(sbFfmpegVersion.ToString(), threadName: LOG_THREAD_NAME);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FATAL: ffmpeg not found/failed to call. Exiting. Exception: {ex.Message}");
                //logger.LogException(ex, "ffmpeg not found/failed to call. Exiting.", threadName: "LOG_THREAD_NAME");
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
