using AutoEncodeClient.Config;
using AutoEncodeUtilities.Config;

namespace AutoEncodeClient;

public static class State
{
    public static ConnectionSettings ConnectionSettings { get; private set; }

    public static LoggerSettings LoggerSettings { get; private set; }

    internal static void LoadFromConfig(ClientConfig config)
    {
        ConnectionSettings = config.ConnectionSettings;
        LoggerSettings = config.LoggerSettings;
    }
}
