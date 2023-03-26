using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeServer.Pipe
{
    public interface IServerPipeManager
    {
        /// <summary>Starts the ServerPipe</summary>
        void Start();

        /// <summary>Stops the ServerPipe</summary>
        void Stop();
    }
}
