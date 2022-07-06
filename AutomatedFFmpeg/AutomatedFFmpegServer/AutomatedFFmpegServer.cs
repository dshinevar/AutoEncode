﻿using AutomatedFFmpegUtilities.Config;
using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Newtonsoft.Json;
using System.Collections.Generic;

namespace AutomatedFFmpegServer
{
    class AutomatedFFmpegServer
    {
        private const string CONFIG_FILE_LOCATION = "AFServerConfig.yaml";

        static void Main(string[] args)
        {
            AFServerConfig serverConfig = null;

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

            AFServerMainThread mainThread = new AFServerMainThread(serverConfig);
            mainThread.Start();

            while (mainThread.IsAlive());
        }
    }
}
