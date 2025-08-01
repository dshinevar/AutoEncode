namespace AutoEncodeUtilities.Config;

public class ConnectionSettings
{
    public string IPAddress { get; set; }
    public int ClientUpdatePort { get; set; }
    public int CommunicationPort { get; set; }
}

/// <summary>Defines settings needed to connect to the server.</summary>
public class ServerConnectionSettings
{
    public int ClientUpdatePort { get; set; }
    public int CommunicationPort { get; set; }
}
