using AutomatedFFmpegClient.Config;
using System;
using System.IO;
using System.Windows;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AutomatedFFmpegClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string CONFIG_FILE_LOCATION = "AFClientConfig.yaml";
        private void AFClient_Startup(object sender, StartupEventArgs e)
        {
            AFClientConfig clientConfig = null;

            try
            {
                using (var reader = new StreamReader(CONFIG_FILE_LOCATION))
                {
                    string str = reader.ReadToEnd();
                    var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();

                    clientConfig = deserializer.Deserialize<AFClientConfig>(str);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Environment.Exit(-2);
            }

            MainWindow wnd = new MainWindow(clientConfig);
            wnd.Show();
        }
    }
}
