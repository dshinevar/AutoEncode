using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Messages;

namespace AutomatedFFmpegServer.ServerSocket
{
    public static class ServerToClientMessageFactory
    {
        public static ClientUpdateMessage CreateClientUpdateMessage(ClientUpdateData data)
        {
            return new ClientUpdateMessage()
            {
                Data = data
            };
        }

        public static ClientConnectMessage CreateClientConnectMessage(ClientConnectData data)
        {
            return new ClientConnectMessage()
            {
                Data = data
            };
        }
    }
}
