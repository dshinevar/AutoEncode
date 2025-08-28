using AutoEncodeServer.Communication.Interfaces;
using AutoEncodeServer.Managers;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeUtilities;
using System;
using System.Threading.Tasks;

namespace AutoEncodeServer;

public partial class AutoEncodeServer :
    ManagerBase,
    IAutoEncodeServer,
    ISourceFileManagerConnection,
    IEncodingJobManagerConnection
{
    #region Managers / Comms
    private IEncodingJobManager EncodingJobManager;

    private ISourceFileManager SourceFileManager;

    public ICommunicationMessageHandler CommunicationMessageHandler { get; set; }
    #endregion Managers / Comms

    /// <summary> Constructor </summary>
    public AutoEncodeServer() { }

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
                Logger.LogException(ex, $"Failed to initialize {nameof(AutoEncodeServer)}", nameof(AutoEncodeServer));
                throw;
            }

            Initialized = true;
            HelperMethods.DebugLog($"{nameof(AutoEncodeServer)} Initialized", nameof(AutoEncodeServer));
        }
    }

    public override async Task Run()
    {
        if (Initialized is false)
            throw new InvalidOperationException($"{nameof(AutoEncodeServer)} is not initialized.");

        HelperMethods.DebugLog($"{nameof(AutoEncodeServer)} Starting", nameof(AutoEncodeServer));

        try
        {
            Task clientUpdatePublisherTask = ClientUpdatePublisher.Run();
            CommunicationMessageHandler.Start();

            Task sourceFileManagerTask = SourceFileManager.Run();
            Task encodingJobMangerTask = EncodingJobManager.Run();

            StartRequestHandler();

            Logger.LogInfo($"{nameof(AutoEncodeServer)} Started", nameof(AutoEncodeServer));
            await Task.WhenAll(sourceFileManagerTask, encodingJobMangerTask, clientUpdatePublisherTask, RequestHandlerTask);
            Logger.LogInfo($"{nameof(AutoEncodeServer)} Shutdown", nameof(AutoEncodeServer));
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to start {nameof(AutoEncodeServer)}");
            throw;
        }
    }

    public override void Shutdown()
    {
        HelperMethods.DebugLog($"{nameof(AutoEncodeServer)} Shutting Down", nameof(AutoEncodeServer));

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
            Logger.LogException(ex, $"Failed to shutdown {nameof(AutoEncodeServer)}", nameof(AutoEncodeServer));
        }
    }
    #endregion Init / Start / Shutdown
}
