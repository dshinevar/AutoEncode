using System;

namespace AutoEncodeClient
{
    public static class Lookups
    {
        public static string LoggerThreadName => "AutoEncodeClient";

        public static string ConfigFileLocation => $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\\AEClient\\AEClientConfig.yaml";

        public static string LogBackupFileLocation => $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\\AEClient";
    }
}
