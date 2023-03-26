using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeClient
{
    public static class Lookups
    {
        public static string ConfigFileLocation => $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\\AEClient\\AEClientConfig.yaml";

        public static string LogBackupFileLocation => $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\\AEClient";
    }
}
