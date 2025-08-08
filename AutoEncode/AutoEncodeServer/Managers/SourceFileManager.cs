using AutoEncodeServer.Factories;
using AutoEncodeServer.Managers.Interfaces;
using AutoEncodeUtilities;
using System;
using System.Linq;

namespace AutoEncodeServer.Managers;

// MAIN
public partial class SourceFileManager :
    ManagerBase,
    ISourceFileManager
{
    #region Dependencies
    public ISourceFileModelFactory SourceFileModelFactory { get; set; }

    public IEncodingJobManagerConnection EncodingJobManagerConnection { get; set; }
    #endregion Dependencies

    /// <summary>Default Constructor</summary>
    public SourceFileManager() { }

    #region Init / Start / Shutdown
    public override void Initialize()
    {
        if (Initialized is false)
        {
            try
            {
                _searchDirectories = State.Directories.ToDictionary(x => x.Key, x => x.Value.DeepClone());  // For now, clone a copy
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

    public override void Shutdown()
    {
        try
        {
            base.Shutdown();
            Wake();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Exception while shutting down {nameof(SourceFileManager)}");
            throw;
        }
    }
    #endregion Init / Start / Shutdown
}
