using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedFFmpegServer
{
    public static class Lookups
    {
        #region LINUX VS WINDOWS
#if DEBUG
        public static string ConfigFileLocation => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "./bin/Debug/net6.0/AFServerConfig.yaml" : "AFServerConfig.yaml";
#else
        public static string ConfigFileLocation = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/usr/local/bin/AFServerConfig.yaml" : "AFServerConfig.yaml";  
#endif
        public static string NullLocation => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/dev/null" : "NUL";
        #endregion LINUX VS WINDOWS

        /// <summary> 
        /// The minimum value to compare against to decide to use X265 or not. 
        /// <para>This value is based on 720p by multiplying the height and width.</para>
        /// </summary>
        public static int MinX265ResolutionInt => 921600;

        public static string PrimaryLanguage => "eng";

        /// <summary>
        /// Audio Codec Priority by "Quality".  The highest index is the best quality and preferred.
        /// <para>Note 1: Readonly for now, may want to make it change in the future. (move to config?)</para>
        /// <para>Note 2: Lookup should be done with a ToLower(). Only lowercase forms are in the list (avoids duplicates).</para>
        /// </summary>
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
