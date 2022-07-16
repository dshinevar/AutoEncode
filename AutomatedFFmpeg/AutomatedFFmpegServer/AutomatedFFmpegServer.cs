using AutomatedFFmpegUtilities.Config;
using AutomatedFFmpegUtilities.Logger;
using System;
using System.IO;
using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using AutomatedFFmpegUtilities;
using System.Text;

namespace AutomatedFFmpegServer
{
    class AutomatedFFmpegServer
    {
        private const string CONFIG_FILE_LOCATION = "AFServerConfig.yaml";

        static void Main(string[] args)
        {
            AFServerMainThread mainThread = null;
            AFServerConfig serverConfig = null;
            ManualResetEvent Shutdown = new ManualResetEvent(false);

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnApplicationExit(sender, e, mainThread, Shutdown);

            Debug.WriteLine("AutomatedFFmpegServer Starting Up.");

            try
            {
                using (var reader = new StreamReader(CONFIG_FILE_LOCATION))
                {
                    string str = reader.ReadToEnd();
                    var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();

                    serverConfig = deserializer.Deserialize<AFServerConfig>(str);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Environment.Exit(-2);
            }

            Debug.WriteLine("Config file loaded.");

            Logger logger = new(serverConfig.ServerSettings.LoggerSettings.LogFileLocation,
                serverConfig.ServerSettings.LoggerSettings.MaxFileSizeInBytes,
                serverConfig.ServerSettings.LoggerSettings.BackupFileCount);

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = $@"{serverConfig.ServerSettings.FFmpegDirectory.RemoveEndingSlashes()}\ffprobe.exe",
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            StringBuilder sbFfprobeOutput = new StringBuilder();

            using (Process ffprobeProcess = new Process())
            {
                ffprobeProcess.StartInfo = startInfo;
                ffprobeProcess.Start();

                using (StreamReader reader = ffprobeProcess.StandardOutput)
                {
                    while (reader.Peek() >= 0)
                    {
                        sbFfprobeOutput.Append(reader.ReadLine());
                    }
                }

                ffprobeProcess.WaitForExit();
            }

            // TODO: Log startup
            // TODO: Check for ffmpeg being installed.

            Debug.WriteLine(sbFfprobeOutput.ToString());

            mainThread = new AFServerMainThread(serverConfig, logger, Shutdown);
            mainThread.Start();

            Shutdown.WaitOne();

            mainThread = null;
        }

        static void OnApplicationExit(object sender, EventArgs e, AFServerMainThread mainThread, ManualResetEvent shutdownMRE)
        {
            if (mainThread is not null)
            {
                mainThread.Shutdown();
                shutdownMRE.WaitOne();
            }
        }
    }
}
