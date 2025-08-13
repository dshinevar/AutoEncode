using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace AutoEncodeServer;

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

    public static string FFmpegExecutable => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
                                                "ffmpeg" :
                                                "ffmpeg.exe";

    public static string FFprobeExecutable => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
                                                "ffprobe" :
                                                "ffprobe.exe";

    public static string HDR10PlusToolExecutable => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
                                                    "hdr10plus_tool" :
                                                    "hdr10plus_tool.exe";

    public static string DoviToolExecutable => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
                                                "dovi_tool" :
                                                "dovi_tool.exe";
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
    public static readonly IList<string> AudioCodecPriority =
    [
        "ac3",
        "aac",
        "dts",
        "dts-es",
        "dts-hd hra",
        "pcm_s16le",
        "pcm_s24le",
        "dts-hd ma",
        "truehd"
    ];
}
