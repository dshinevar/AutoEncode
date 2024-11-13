using AutoEncodeUtilities.Config;

namespace AutoEncodeClient.Config;

public class ClientConfig
{
    public ConnectionSettings ConnectionSettings { get; set; }

    public LoggerSettings LoggerSettings { get; set; }
}
