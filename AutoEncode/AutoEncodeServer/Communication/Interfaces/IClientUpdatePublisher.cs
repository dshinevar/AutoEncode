using AutoEncodeUtilities.Communication.Data;
using System.Threading;

namespace AutoEncodeServer.Communication.Interfaces;

public interface IClientUpdatePublisher
{
    #region Properties
    string ConnectionString { get; }

    int Port { get; }
    #endregion Properties

    #region Initialize / Start / Shutdown
    /// <summary>Sets up the <see cref="ClientUpdatePublisher"/> </summary>
    /// <param name="shutdownMRE"><see cref="ManualResetEvent"/> used to indicate when shut down</param>
    void Initialize(ManualResetEvent shutdownMRE);

    /// <summary>Binds connection and starts up request thread.</summary>
    void Start();

    /// <summary>Unbinds connection and stops threads. </summary>
    void Stop();
    #endregion Initialize / Start / Shutdown

    /// <summary>Adds a client update request to the queue.</summary>
    /// <param name="topic">The topic the client(s) should be subscribed to.</param>
    /// <param name="message">The update message.</param>
    /// <returns>True if added to queue.</returns>
    bool AddClientUpdateRequest(string topic, ClientUpdateMessage message);
}
