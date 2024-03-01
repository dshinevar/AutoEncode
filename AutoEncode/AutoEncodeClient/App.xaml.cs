using AutoEncodeClient.Communication;
using AutoEncodeClient.Config;
using AutoEncodeClient.Models;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.ViewModels;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeClient.Views;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Logger;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
        private ILogger Logger = null;
        private WindsorContainer _container;
        private ICommunicationManager _communicatorManager;
        private IClientUpdateSubscriber _clientUpdateSubscriber;

        private void AEClient_Startup(object sender, StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            // Container Standup
            _container = new();
            RegisterContainerComponents(_container);

            AEClientConfig clientConfig = _container.Resolve<AEClientConfig>();
            try
            {
                using var reader = new StreamReader(Lookups.ConfigFileLocation);
                string str = reader.ReadToEnd();
                var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();

                deserializer.Deserialize<AEClientConfig>(str).CopyProperties(clientConfig);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Application.Current.Shutdown(-2);
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
                        Application.Current.Shutdown(-2);
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
                        Application.Current.Shutdown(-2);
                    }

                    LogFileLocation = Lookups.LogBackupFileLocation;
                }
                else
                {
                    // Exception we don't want to handle, exit.
                    Application.Current.Shutdown(-2);
                }
            }

            Logger = _container.Resolve<ILogger>();
            Logger.Initialize(LogFileLocation, LOG_FILENAME, clientConfig.LoggerSettings.MaxFileSizeInBytes, clientConfig.LoggerSettings.BackupFileCount);
            if (Logger.CheckAndDoRollover() is false)
            {
                // If rollover fails, just exit
                Application.Current.Shutdown(-2);
            }

            Current.Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            // Build and show view
            try
            {
                _communicatorManager = _container.Resolve<ICommunicationManager>();
                _communicatorManager.Initialize(clientConfig.ConnectionSettings.IPAddress, clientConfig.ConnectionSettings.CommunicationPort);

                _clientUpdateSubscriber = _container.Resolve<IClientUpdateSubscriber>();
                _clientUpdateSubscriber.Initialize(clientConfig.ConnectionSettings.IPAddress, clientConfig.ConnectionSettings.ClientUpdatePort);
                _clientUpdateSubscriber.Start();

                IAutoEncodeClientModel clientModel = _container.Resolve<IAutoEncodeClientModel>();   // Model currently doesn't do anything

                IAutoEncodeClientViewModel viewModel = _container.Resolve<IAutoEncodeClientViewModel>();
                viewModel.Initialize(clientModel);

                AutoEncodeClientView view = new(viewModel)
                {
                    Title = $"AutoEncodeClient - {Assembly.GetExecutingAssembly().GetName().Version}"
                };
                Application.Current.MainWindow = view;
                view.Show();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Crash - AutoEncodeClient Shutting Down", Lookups.LoggerThreadName);
                Application.Current.Shutdown();
            }    
        }

        private void AEClient_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                _clientUpdateSubscriber?.Stop();

                _container.Release(_communicatorManager);
                _container.Release(_clientUpdateSubscriber);
            }
            catch (Exception ex) 
            {
                Logger?.LogException(ex, "Error on AutoEncodeClient exit.", Lookups.LoggerThreadName);
                Environment.Exit(-2);
            }
        }

        private static void RegisterContainerComponents(WindsorContainer container)
        {
            // Top Level Setup
            container.Install(FromAssembly.This());

            container.AddFacility<TypedFactoryFacility>();

            // Register Container to be accessed later
            container.Register(Component.For<IWindsorContainer>()
                .Instance(container));

            // Register Components
            container.Register(Component.For<ILogger>()
                .ImplementedBy<Logger>()
                .LifestyleSingleton());

            container.Register(Component.For<ICommunicationManager>()
                .ImplementedBy<CommunicationManager>()
                .LifestyleSingleton());

            container.Register(Component.For<IClientUpdateSubscriber>()
                .ImplementedBy<ClientUpdateSubscriber>()
                .LifestyleSingleton());

            container.Register(Component.For<AEClientConfig>()
                .LifestyleSingleton());

            container.Register(Component.For<IEncodingJobClientModelFactory>()
                .AsFactory());

            container.Register(Component.For<ISourceFilesViewModel>()
                .ImplementedBy<SourceFilesViewModel>()
                .LifestyleSingleton()
                .OnCreate(instance => instance.RefreshSourceFiles()));

            container.Register(Component.For<IAutoEncodeClientModel>()
                .ImplementedBy<AutoEncodeClientModel>()
                .LifestyleSingleton());

            container.Register(Component.For<IAutoEncodeClientViewModel>()
                .ImplementedBy<AutoEncodeClientViewModel>()
                .LifestyleSingleton());

            container.Register(Component.For<IEncodingJobClientModel>()
                .ImplementedBy<EncodingJobClientModel>()
                .LifestyleTransient()
                .OnCreate(model => model.Initialize()));
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
            => Logger?.LogException(e.Exception, "Unhandled Dispatcher Exception");

    }
}
