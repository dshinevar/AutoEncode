using AutomatedFFmpegUtilities.Config;
using System;
using System.IO;
using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;

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

            mainThread = new AFServerMainThread(serverConfig, Shutdown);
            mainThread.Start();

            Shutdown.WaitOne();

            mainThread = null;
        }

        static void OnApplicationExit(object sender, EventArgs e, AFServerMainThread mainThread, ManualResetEvent shutdownMRE)
        {
            mainThread.Shutdown();
            shutdownMRE.WaitOne();
            mainThread = null;
        }
    }
}
