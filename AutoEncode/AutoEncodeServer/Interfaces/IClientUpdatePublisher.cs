using System;

namespace AutoEncodeServer.Interfaces
{
    public interface IClientUpdatePublisher
    {
        int Port { get; }

        string ConnectionString { get; }

        /// <summary>Sends given data to clients for the given topic</summary>
        /// <param name="topic">Topic of data</param>
        /// <param name="data">Data sent</param>
        void SendUpdateToClients(string topic, object data);

        /// <summary>Sets up data (port)</summary>
        /// <param name="port">Port to bind to.</param>
        void Initialize(int port);

        /// <summary>Sets up and binds publisher.</summary>
        /// <exception cref="Exception">Rethrows Exception if occured</exception>
        void Start();

        /// <summary>Closes and disposes publisher. </summary>
        /// <exception cref="Exception">Rethrows Exception if occured</exception>
        void Shutdown();
    }
}
