using AutoEncodeUtilities.Communication.Data;
using AutoEncodeUtilities.Communication.Enums;
using System;
using System.Collections.Generic;

namespace AutoEncodeClient.Communication.Interfaces;

public interface IClientUpdateSubscriber
{
    event EventHandler<CommunicationMessage<ClientUpdateType>> ClientUpdateMessageReceived;

    bool Initialized { get; }

    string ConnectionString { get; }

    string IpAddress { get; }

    int Port { get; }

    #region Init / Start / Stop
    /// <summary>Initializes connection data.</summary>
    void Initialize();

    /// <summary>Starts connection and listening for messages.</summary>
    void Start();

    /// <summary>Stops listening and disconnects.</summary>
    void Stop();
    #endregion Init / Start / Stop

    #region Subscribe / Unsubscribe
    /// <summary>Subscribes to the given topic.</summary>
    /// <param name="topic">Subscription topic.</param>
    void Subscribe(string topic);

    /// <summary>Subscribes to the given list of topics.</summary>
    /// <param name="topics">List of topics.</param>
    void Subscribe(IEnumerable<string> topics);

    /// <summary>Unsubscribes to the given topic.</summary>
    /// <param name="topic">Subscription topic.</param>
    void Unsubscribe(string topic);

    /// <summary>Unsubscribes to the given list of topics.</summary>
    /// <param name="topics">List of topics.</param>
    void Unsubscribe(IEnumerable<string> topics);
    #endregion Subscribe / Unsubscribe
}
