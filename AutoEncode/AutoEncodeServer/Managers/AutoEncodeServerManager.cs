using AutoEncodeServer.Communication.Interfaces;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeUtilities;
using System;
using System.Threading.Tasks;

namespace AutoEncodeServer.Managers;

public partial class AutoEncodeServerManager :
    ManagerBase,
    IAutoEncodeServerManager,
    ISourceFileManagerConnection,
    IEncodingJobManagerConnection
{
    #region Managers / Comms
    private IEncodingJobManager EncodingJobManager;

    private ISourceFileManager SourceFileManager;

    public ICommunicationMessageHandler CommunicationMessageHandler { get; set; }
    #endregion Managers / Comms

    /// <summary> Constructor </summary>
    public AutoEncodeServerManager() { }

    #region Init / Start / Shutdown
    public override void Initialize()
    {
        if (Initialized is false)
        {
            try
            {
                CommunicationMessageHandler.MessageReceived += CommunicationMessageHandler_MessageReceived;

                SourceFileManager = Container.Resolve<ISourceFileManager>();
                EncodingJobManager = Container.Resolve<IEncodingJobManager>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to initialize {nameof(AutoEncodeServerManager)}", nameof(AutoEncodeServerManager));
                throw;
            }

            Initialized = true;
            HelperMethods.DebugLog($"{nameof(AutoEncodeServerManager)} Initialized", nameof(AutoEncodeServerManager));
        }
    }

    public override async Task Run()
    {
        if (Initialized is false)
            throw new InvalidOperationException($"{nameof(AutoEncodeServerManager)} is not initialized.");

        HelperMethods.DebugLog($"{nameof(AutoEncodeServerManager)} Starting", nameof(AutoEncodeServerManager));

        try
        {
            Task clientUpdatePublisherTask = ClientUpdatePublisher.Run();
            CommunicationMessageHandler.Start();

            Task sourceFileManagerTask = SourceFileManager.Run();
            Task encodingJobMangerTask = EncodingJobManager.Run();

            StartRequestHandler();

            Logger.LogInfo($"{nameof(AutoEncodeServerManager)} Started", nameof(AutoEncodeServerManager));
            await Task.WhenAll(sourceFileManagerTask, encodingJobMangerTask, clientUpdatePublisherTask, RequestHandlerTask);
            Logger.LogInfo($"{nameof(AutoEncodeServerManager)} Shutdown", nameof(AutoEncodeServerManager));
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to start {nameof(AutoEncodeServerManager)}");
            throw;
        }
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
            CommunicationMessageHandler?.Stop();
            ClientUpdatePublisher?.Stop();

            // Stop threads
            SourceFileManager?.Shutdown();
            EncodingJobManager?.Shutdown();
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
