using AutoEncodeServer.Factories;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeUtilities;
using System;
using System.Linq;
using System.Threading;

namespace AutoEncodeServer.Managers;

// MAIN
public partial class SourceFileManager :
    ManagerBase,
    ISourceFileManager
{
    #region Dependencies
    public ISourceFileModelFactory SourceFileModelFactory { get; set; }

    private IEncodingJobManager _encodingJobManager;
    #endregion Dependencies

    public bool Initialized { get; set; }

    /// <summary>Default Constructor</summary>
    public SourceFileManager() { }

    #region Init / Start / Shutdown
    public override void Initialize(ManualResetEvent shutdownMRE)
    {
        if (Initialized is false)
        {
            try
            {
                ShutdownMRE = shutdownMRE;
                ShutdownMRE.Reset();
                _searchDirectories = State.Directories.ToDictionary(x => x.Key, x => x.Value.DeepClone());  // For now, clone a copy

                _encodingJobManager = Container.Resolve<IEncodingJobManager>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to initialize {nameof(SourceFileManager)}", nameof(SourceFileManager));
                throw;
            }
        }

        Initialized = true;
        HelperMethods.DebugLog($"{nameof(SourceFileManager)} Initialized", nameof(SourceFileManager));
    }

    public override void Start()
    {
        try
        {
            if (Initialized is false)
                throw new InvalidOperationException($"{nameof(SourceFileManager)} is not initialized.");

            StartManagerProcess();
            StartRequestHandler();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to start {nameof(SourceFileManager)}", nameof(SourceFileManager));
            throw;
        }

        Logger.LogInfo($"{nameof(SourceFileManager)} Started", nameof(SourceFileManager));
    }

    public override void Shutdown()
    {
        try
        {
            Requests.CompleteAdding();

            ShutdownCancellationTokenSource.Cancel();

            Wake();

            ManagerProcessTask?.Wait();
            RequestHandlerTask?.Wait();

            Logger.LogInfo($"{nameof(SourceFileManager)} Shutdown", nameof(SourceFileManager));

            ShutdownMRE.Set();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to shutdown {nameof(SourceFileManager)}", nameof(SourceFileManager));
            throw;
        }
    }
    #endregion Init / Start / Shutdown
}
