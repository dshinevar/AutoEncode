using AutoEncodeServer.Factories;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeUtilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    private bool _initialized = false;

    private Task _sourceFileManagerTask;
    private ManualResetEvent _shutdownMRE;

    /// <summary>Default Constructor</summary>
    public SourceFileManager() { }

    #region Init / Start / Stop
    public void Initialize(ManualResetEvent shutdownMRE)
    {
        if (_initialized is false)
        {
            try
            {
                _shutdownMRE = shutdownMRE;
                _shutdownMRE.Reset();
                _searchDirectories = State.Directories.ToDictionary(x => x.Key, x => x.Value.DeepClone());  // For now, clone a copy

                _encodingJobManager = Container.Resolve<IEncodingJobManager>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to initialize {nameof(SourceFileManager)}", nameof(SourceFileManager));
                throw;
            }
        }

        _initialized = true;
        HelperMethods.DebugLog($"{nameof(SourceFileManager)} Initialized", nameof(SourceFileManager));
    }

    public void Start()
    {
        try
        {
            if (_initialized is false)
                throw new InvalidOperationException($"{nameof(SourceFileManager)} is not initialized.");

            StartSourceFileManagerThread();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to start {nameof(SourceFileManager)}", nameof(SourceFileManager));
            throw;
        }

        Logger.LogInfo($"{nameof(SourceFileManager)} Started", nameof(SourceFileManager));
    }

    public void Stop()
    {
        try
        {
            ShutdownCancellationTokenSource.Cancel();

            Wake();

            _sourceFileManagerTask.Wait();
            RequestHandlerTask?.Wait();

            Logger.LogInfo($"{nameof(SourceFileManager)} Stopped", nameof(SourceFileManager));

            _shutdownMRE.Set();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Failed to stop {nameof(SourceFileManager)}", nameof(SourceFileManager));
            throw;
        }
    }
    #endregion Init / Start / Stop
}
