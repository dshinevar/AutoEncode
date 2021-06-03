using AutomatedFFmpegUtilities.Config;
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

        private class Test
        {
            public List<Frames> frames;
        }

        public class Frames
        {
            public string media_type;
            public int stream_index;
            public List<SideDataListMastering> side_data_list;
        }

        public class SideDataList
        {
            public string side_data_type;
        }

        public class SideDataListMastering : SideDataList
        {
            public string red_x;
            public int max_content;
        }

        public class SideDataListContent : SideDataList
        {
            public int max_content;
        }



        static void Main(string[] args)
        {
            Console.WriteLine("Server Start");
            AFServerConfig serverConfig = null;

            //Test o = JsonConvert.DeserializeObject<Test>(File.ReadAllText("Y:\\plex-media\\Hereditary.json"));

            //SideDataList s = o.frames[1].side_data_list.Find(x => x.side_data_type == "Mastering display metadata");

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
