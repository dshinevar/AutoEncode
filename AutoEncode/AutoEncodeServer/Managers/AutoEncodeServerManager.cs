using AutoEncodeServer.Communication.Interfaces;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeUtilities;
using System;
using System.Threading;

namespace AutoEncodeServer.Managers;

public partial class AutoEncodeServerManager :
    ManagerBase,
    IAutoEncodeServerManager
{
    private bool _initialized = false;

    // MREs are true by default -- initializers should reset the MRE
    private readonly ManualResetEvent _sourceFileManagerShutdown = new(false);
    private readonly ManualResetEvent _encodingJobManagerShutdown = new(false);
    private readonly ManualResetEvent _clientUpdatePublisherShutdown = new(false);
    private readonly ManualResetEvent _communicationMessageHandlerShutdown = new(false);

    #region Managers / Comms
    private IEncodingJobManager _encodingJobManager;

    private ISourceFileManager _sourceFileManager;

    private IClientUpdatePublisher _clientUpdatePublisher;

    private ICommunicationMessageHandler _communicationMessageHandler;
    #endregion Managers / Comms

    /// <summary> Constructor </summary>
    public AutoEncodeServerManager() { }

    #region Init / Start / Shutdown
    public override void Initialize(ManualResetEvent shutdown)
    {
        if (_initialized is false)
        {
            try
            {
                ShutdownMRE = shutdown;

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

    public override void Start()
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

            // DO NOT CALL StartManagerProcess()
            StartRequestHandler();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to start {nameof(AutoEncodeServerManager)}", nameof(AutoEncodeServerManager));
            throw;
        }

        Logger.LogInfo($"{nameof(AutoEncodeServerManager)} Started", nameof(AutoEncodeServerManager));
    }

    public override void Shutdown()
    {
        HelperMethods.DebugLog($"{nameof(AutoEncodeServerManager)} Shutting Down", nameof(AutoEncodeServerManager));

        try
        {
            Requests.CompleteAdding();

            // Shutdown this manager's threads (message processor)
            ShutdownCancellationTokenSource.Cancel();

            // Stop Comms
            _clientUpdatePublisher?.Stop();
            _communicationMessageHandler?.Stop();

            // Stop threads
            _sourceFileManager?.Shutdown();
            _encodingJobManager?.Shutdown();

            // Wait for threads to stop
            try
            {
                RequestHandlerTask?.Wait();
            }
            catch (OperationCanceledException) { }

            _clientUpdatePublisherShutdown.WaitOne();
            _communicationMessageHandlerShutdown.WaitOne();
            _encodingJobManagerShutdown.WaitOne();
            _sourceFileManagerShutdown.WaitOne();

            Logger.LogInfo($"{nameof(AutoEncodeServerManager)} Shutdown", nameof(AutoEncodeServerManager));
            ShutdownMRE.Set();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to shutdown {nameof(AutoEncodeServerManager)}", nameof(AutoEncodeServerManager));
        }
    }
    #endregion Init / Start / Shutdown

    protected override void Process()
    {
        // Not used currently
        throw new NotImplementedException();
    }
}
