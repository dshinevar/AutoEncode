using AutoEncodeServer.Communication.Interfaces;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Logger;
using Castle.Windsor;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.Managers;

public partial class AutoEncodeServerManager : IAutoEncodeServerManager
{
    private bool _initialized = false;

    private Task _messageProcessingTask = null;
    private ManualResetEvent _shutdownMRE = null;

    // MREs are true by default -- initializers should reset the MRE
    private readonly ManualResetEvent _sourceFileManagerShutdown = new(true);
    private readonly ManualResetEvent _encodingJobManagerShutdown = new(true);
    private readonly ManualResetEvent _clientUpdatePublisherShutdown = new(true);
    private readonly ManualResetEvent _communicationMessageHandlerShutdown = new(true);
    private readonly CancellationTokenSource _shutdownCancellationTokenSource = new();

    #region Dependencies
    public IWindsorContainer Container { get; set; }

    public ILogger Logger { get; set; }
    #endregion Dependencies

    #region Managers / Comms
    private IEncodingJobManager _encodingJobManager;

    private ISourceFileManager _sourceFileManager;

    private IClientUpdatePublisher _clientUpdatePublisher;

    private ICommunicationMessageHandler _communicationMessageHandler;
    #endregion Managers / Comms

    /// <summary> Constructor </summary>
    public AutoEncodeServerManager() { }

    #region Init / Start / Shutdown
    public void Initialize(ManualResetEvent shutdown)
    {
        if (_initialized is false)
        {
            try
            {
                _shutdownMRE = shutdown;

                // Initialize comms first
                _clientUpdatePublisher = Container.Resolve<IClientUpdatePublisher>();
                _clientUpdatePublisher.Initialize(_clientUpdatePublisherShutdown);

                _communicationMessageHandler = Container.Resolve<ICommunicationMessageHandler>();
                _communicationMessageHandler.Initialize(_communicationMessageHandlerShutdown);
                _communicationMessageHandler.MessageReceived += CommunicationMessageHandler_MessageReceived;

                // Create managers
                _sourceFileManager = Container.Resolve<ISourceFileManager>();
                _encodingJobManager = Container.Resolve<IEncodingJobManager>();

                _sourceFileManager.Initialize(_sourceFileManagerShutdown);
                _encodingJobManager.Initialize(_encodingJobManagerShutdown);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to initialize {nameof(AutoEncodeServerManager)}", nameof(AutoEncodeServerManager));
                throw;
            }

            _initialized = true;
            HelperMethods.DebugLog($"{nameof(AutoEncodeServerManager)} Initialized", nameof(AutoEncodeServerManager));
        }
    }

    public void Start()
    {
        if (_initialized is false)
            throw new InvalidOperationException($"{nameof(AutoEncodeServerManager)} is not initialized.");

        HelperMethods.DebugLog($"{nameof(AutoEncodeServerManager)} Starting", nameof(AutoEncodeServerManager));

        try
        {
            // Handle comms first
            _clientUpdatePublisher.Start();
            _communicationMessageHandler.Start();

            _sourceFileManager.Start();
            _encodingJobManager.Start();

            StartMessageProcessor();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to start {nameof(AutoEncodeServerManager)}", nameof(AutoEncodeServerManager));
            throw;
        }

        Logger.LogInfo($"{nameof(AutoEncodeServerManager)} Started", nameof(AutoEncodeServerManager));
    }

    public void Shutdown()
    {
        HelperMethods.DebugLog($"{nameof(AutoEncodeServerManager)} Stopping", nameof(AutoEncodeServerManager));

        try
        {
            // Shutdown this manager's threads (message processor)
            _shutdownCancellationTokenSource.Cancel();

            // Stop Comms
            _clientUpdatePublisher?.Stop();
            _communicationMessageHandler?.Stop();

            // Stop threads
            _sourceFileManager?.Stop();
            _encodingJobManager?.Stop();

            // Wait for threads to stop
            try
            {
                _messageProcessingTask?.Wait();
            }
            catch (OperationCanceledException) { }
            
            _clientUpdatePublisherShutdown.WaitOne();
            _communicationMessageHandlerShutdown.WaitOne();
            _encodingJobManagerShutdown.WaitOne();
            _sourceFileManagerShutdown.WaitOne();

            Logger.LogInfo($"{nameof(AutoEncodeServerManager)} Stopped", nameof(AutoEncodeServerManager));
            _shutdownMRE.Set();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to shutdown {nameof(AutoEncodeServerManager)}", nameof(AutoEncodeServerManager));
        }
    }
    #endregion Init / Start / Shutdown
}
