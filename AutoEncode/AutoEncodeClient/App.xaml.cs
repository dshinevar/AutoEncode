using AutoEncodeClient.Config;
using AutoEncodeClient.Models;
using AutoEncodeClient.ViewModels;
using AutoEncodeClient.Views;
using AutoEncodeUtilities.Config;
using AutoEncodeUtilities.Logger;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoEncodeClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string LOG_FILENAME = "aeclient.log";
        private ILogger logger = null;
        private void AEClient_Startup(object sender, StartupEventArgs e)
        {
            AEClientConfig clientConfig = null;
            try
            {
                using var reader = new StreamReader(Lookups.ConfigFileLocation);
                string str = reader.ReadToEnd();
                var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();

                clientConfig = deserializer.Deserialize<AEClientConfig>(str);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Environment.Exit(-2);
            }

            Debug.WriteLine("Config file loaded.");

            // CREATE LOG FILE DIRECTORY
            string LogFileLocation = clientConfig.LoggerSettings.LogFileLocation;
            try
            {
                DirectoryInfo directoryInfo = Directory.CreateDirectory(clientConfig.LoggerSettings.LogFileLocation);

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

            logger = new Logger(LogFileLocation,
                                            LOG_FILENAME,
                                            clientConfig.LoggerSettings.MaxFileSizeInBytes,
                                            clientConfig.LoggerSettings.BackupFileCount);

            Current.Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            // Build and show view
            try
            {
                AutoEncodeClientModel model = new(logger, clientConfig);
                AutoEncodeClientViewModel viewModel = new(model, logger, clientConfig);
                AutoEncodeClientView view = new(viewModel);
                view.Show();
            }
            catch (Exception ex) 
            {
                logger.LogException(ex, "Crash - AutoEncodeClient Shutting Down", Lookups.LoggerThreadName);
            }
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
            => logger?.LogException(e.Exception, "Unhandled Dispatcher Exception");
    }
}
