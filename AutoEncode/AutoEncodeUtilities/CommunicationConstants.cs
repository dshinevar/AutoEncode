using Newtonsoft.Json;

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
