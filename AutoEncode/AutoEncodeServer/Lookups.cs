using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace AutoEncodeServer
{
    public static class Lookups
    {
        #region LINUX VS WINDOWS
        public static string ConfigFileLocation => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
                                                    "/etc/aeserver/AEServerConfig.yaml" :
                                                    $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\\AEServer\\AEServerConfig.yaml";
        public static string NullLocation => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/dev/null" : "NUL";

        public static string LogBackupFileLocation => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
                                    @"/var/log/aeserver" :
                                    $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\\AEServer";

        public static string PreviouslyEncodingTempFile => $"{Path.GetTempPath()}aeserver.tmp";
        #endregion LINUX VS WINDOWS

        /// <summary> 
        /// The minimum value to compare against to decide to use X265 or not. 
        /// <para>This value is based on 720p by multiplying the height and width.</para>
        /// </summary>
        public static int MinX265ResolutionInt => 921600;

        public static string PrimaryLanguage => "eng";

        /// <summary> Audio Codec Priority by "Quality".  The highest index is the best quality and preferred.</summary>
        /// <remarks>Note 1: Readonly for now, may want to make it change in the future. (move to config?)
        /// <para>Note 2: Lookup should be done with a ToLower(). Only lowercase forms are in the list (avoids duplicates).</para></remarks>
        public static readonly IList<string> AudioCodecPriority = new List<string>()
        {
            "ac3",
            "dts",
            "dts-es",
            "dts-hd hra",
            "pcm_s16le",
            "pcm_s24le",
            "dts-hd ma",
            "truehd"
        };
    }
}
