namespace AutoEncodeUtilities.Config;

/// <summary>Settings and info related to ffmpeg/ffprobe.</summary>
public class FfmpegSettings
{
    /// <summary>The directory ffmpeg should be located in.</summary>
    public string FfmpegDirectory { get; set; } = string.Empty;

    /// <summary>The directory ffprobe should be located in.</summary>
    public string FfprobeDirectory { get; set; } = string.Empty;

    /// <summary>Enables the usage of nnedi deinterlacing filter.</summary>
    public bool NnediEnabled { get; set; } = false;

    /// <summary>The directory where the required for nnedi nnedi3_weights.bin file is located</summary>
    public string NnediDirectory { get; set; } = string.Empty;
}

/// <summary>Settings and info related to HDR10+ encoding. </summary>
public class Hdr10PlusSettings
{
    /// <summary>Flag dicating if HDR10+ encoding should be used at all.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>The full path of hdr10plus_tool (third-party application) </summary>
    /// <remarks>Used for extracting hdr10+ metadata (https://github.com/quietvoid/hdr10plus_tool)</remarks>
    public string Hdr10PlusToolFullPath { get; set; } = string.Empty;
}

/// <summary>Settings and info related to DolbyVision encoding. </summary>
public class DolbyVisionSettings
{
    /// <summary>Flag dicating if DolbyVision encoding should be used at all.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>The full path to dovi_tool (third-party application) </summary>
    /// <remarks>Used for extracting DolbyVision metadata (https://github.com/quietvoid/dovi_tool)</remarks>
    public string DoviToolFullPath { get; set; } = string.Empty;

    /// <summary>The full path to x265 application</summary>
    /// <remarks>Used for encoding video for DolbyVision. TODO: CHECK FOR NEW DOLBYVISION FFMPEG FLAG</remarks>
    public string X265FullPath { get; set; } = string.Empty;

    /// <summary>The full path to mkvmerge.</summary>
    /// <remarks>Used for merging encoded video and audio/subs back together.</remarks>
    public string MkvMergeFullPath { get; set; } = string.Empty;
}
