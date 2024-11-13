namespace AutoEncodeUtilities.Config;

public class LoggerSettings
{
    public string LogFileDirectory { get; set; }

    public long MaxFileSizeInBytes { get; set; }

    public int BackupFileCount { get; set; }
}
