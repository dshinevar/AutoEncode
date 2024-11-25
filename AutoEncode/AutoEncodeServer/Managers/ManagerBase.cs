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
    protected readonly CancellationTokenSource ShutdownCancellationTokenSource = new();
    protected ManualResetEvent ShutdownMRE = null;
    #endregion Properties / Fields


    #region Init / Start / Shutdown
    public abstract void Initialize(ManualResetEvent shutdownMRE);

    public abstract void Start();

    public abstract void Shutdown();
    #endregion Init / Start / Shutdown


    #region Manager Process
    protected Task ManagerProcessTask = null;

    protected Task StartManagerProcess()
        => ManagerProcessTask = Task.Run(Process, ShutdownCancellationTokenSource.Token);

    protected abstract void Process();
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
                        Logger.LogException(ex, "Error processing request.", this.GetType().Name);
                    }
                }
            }
            catch (OperationCanceledException) { }
        }
    }
    #endregion Request Handling
}
