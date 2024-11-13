using AutoEncodeServer.Communication.Interfaces;
using AutoEncodeServer.Data.Request;
using AutoEncodeUtilities.Logger;
using Castle.Windsor;
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

    protected readonly CancellationTokenSource ShutdownCancellationTokenSource = new();

    #region Request Handling
    private const int _requestHandlerTimeout = 3600000;   // 1 hour
    protected Task RequestHandlerTask;
    protected BlockingCollection<ManagerRequest> Requests { get; } = [];

    /// <summary>Starts the Request Handler thread.</summary>
    /// <returns><see cref="Task"/> -- <see cref="RequestHandlerTask"/></returns>
    protected Task StartRequestHandler()
        => RequestHandlerTask = Task.Run(() =>
        {
            while (Requests.TryTake(out ManagerRequest request, _requestHandlerTimeout, ShutdownCancellationTokenSource.Token))
            {
                ProcessManagerRequest(request);
            }
        }, ShutdownCancellationTokenSource.Token);

    /// <summary>Attempts to add request to queue -- starts up request handler if not running. </summary>
    /// <param name="request"><see cref="ManagerRequest"/> to process</param>
    /// <returns>True if added; False, otherwise.</returns>
    protected bool TryAddRequest(ManagerRequest request)
    {
        if ((RequestHandlerTask is null) ||
            (RequestHandlerTask.Status != TaskStatus.Running) ||
            (RequestHandlerTask.IsCompleted is true))
        {
            StartRequestHandler();
        }

        return Requests.TryAdd(request);
    }

    protected abstract void ProcessManagerRequest(ManagerRequest request);
    #endregion Request Handling
}
