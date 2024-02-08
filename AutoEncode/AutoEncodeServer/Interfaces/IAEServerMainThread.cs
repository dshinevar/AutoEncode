using AutoEncodeUtilities.Config;
using System;
using System.Threading;

namespace AutoEncodeServer.Interfaces
{
    /// <summary>Main Thread that handles startup and shutdown of server</summary>
    public interface IAEServerMainThread
    {
        /// <summary>Initializes the server</summary>
        /// <param name="serverState">State parameters to use</param>
        /// <param name="serverConfig">Config file</param>
        /// <param name="shutdown">Shutdown <see cref="ManualResetEvent"/></param>
        /// <exception cref="Exception">Rethrows Exception</exception>
        void Initialize(AEServerConfig serverState, AEServerConfig serverConfig, ManualResetEvent shutdown);

        /// <summary>Starts the threads, timers, and communication services </summary>
        /// <exception cref="Exception">Rethrows Exception</exception>
        void Start();

        /// <summary>Shuts down threads, timers, and communication services </summary>
        void Shutdown();
    }
}
