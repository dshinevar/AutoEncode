using AutoEncodeServer.Communication.Interfaces;
using AutoEncodeUtilities.Logger;
using Castle.Windsor;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AutoEncodeServer.Managers;

public abstract class ManagerBase
{
    #region Dependencies
    public IWindsorContainer Container { get; set; }

    public ILogger Logger { get; set; }

    public IClientUpdatePublisher ClientUpdatePublisher { get; set; }
    #endregion Dependencies


    #region Properties / Fields
    public bool Initialized { get; protected set; }
    protected readonly CancellationTokenSource ShutdownCancellationTokenSource = new();
    #endregion Properties / Fields


    #region Init / Start / Shutdown
    public virtual void Initialize() => Initialized = true;

    public virtual Task Run()
    {
        try
        {
            if (Initialized is false)
                throw new InvalidOperationException($"{GetType().Name} is not initialized.");

            StartManagerProcess();
            StartRequestHandler();

            Logger.LogInfo($"{GetType().Name} Started", GetType().Name);

            return Task.WhenAll(ManagerProcessTask, RequestHandlerTask)
                        .ContinueWith((task) => Logger.LogInfo($"{GetType().Name} Shutdown", GetType().Name));
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Exception while running {GetType().Name}", GetType().Name);
            throw;
        }
    }

    public virtual void Shutdown()
    {
        try
        {
            Requests.CompleteAdding();                  // Stop Requests
            ShutdownCancellationTokenSource.Cancel();   // Initiate shutdown by stopping processes
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, $"Exception while shutting down {GetType().Name}", GetType().Name);
            throw;
        }
    }
    #endregion Init / Start / Shutdown


    #region Manager Process
    protected Task ManagerProcessTask = null;

    protected void StartManagerProcess()
        => ManagerProcessTask = Task.Run(Process, ShutdownCancellationTokenSource.Token);

    protected virtual void Process() => throw new NotImplementedException("Not currently implemented.");
    #endregion Manager Process


    #region Request Handling
    protected Task RequestHandlerTask = null;
    protected BlockingCollection<Action> Requests { get; } = [];

    /// <summary>Starts the Request Handler thread.</summary>
    /// <returns><see cref="Task"/> -- <see cref="RequestHandlerTask"/></returns>
    protected void StartRequestHandler()
    {
        RequestHandlerTask = Task.Run(ProcessRequests, ShutdownCancellationTokenSource.Token);

        void ProcessRequests()
        {
            try
            {
                foreach (Action request in Requests.GetConsumingEnumerable(ShutdownCancellationTokenSource.Token))
                {
                    try
                    {
                        request();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex, "Error processing request.", GetType().Name);
                    }
                }
            }
            catch (OperationCanceledException) { }
        }
    }
    #endregion Request Handling
}
