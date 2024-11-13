using System;
using System.Threading;

namespace AutoEncodeServer.Managers.Interfaces;

public interface IAutoEncodeServerManager
{
    /// <summary>Initializes the server</summary>
    /// <param name="shutdown">Shutdown <see cref="ManualResetEvent"/></param>
    /// <exception cref="Exception">Rethrows Exception</exception>
    void Initialize(ManualResetEvent shutdown);

    /// <summary>Starts the threads, timers, and communication services </summary>
    /// <exception cref="Exception">Rethrows Exception</exception>
    void Start();

    /// <summary>Shuts down threads, timers, and communication services </summary>
    void Shutdown();
}
