using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeUtilities
{
    public static class CommunicationConstants
    {
        public static readonly JsonSerializerSettings SerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public const string ClientUpdateTopic = "ClientUpdate";
    }
}
