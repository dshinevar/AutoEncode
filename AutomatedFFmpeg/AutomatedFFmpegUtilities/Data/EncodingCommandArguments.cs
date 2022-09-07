using AutomatedFFmpegUtilities.Interfaces;

namespace AutomatedFFmpegUtilities.Data
{
    /// <summary>Basic FFmpegEncodingCommandArguments</summary>
    public class EncodingCommandArguments : IEncodingCommandArguments
    {
        public string FFmpegEncodingCommandArguments { get; set; }
    }

    /// <summary>Advanced arguments for DolbyVision encoding</summary>
    public class DolbyVisionEncodingCommandArguments : IEncodingCommandArguments
    {
        public string VideoEncodingCommandArguments { get; set; }
        public string AudioSubsEncodingCommandArguments { get; set; }
        public string MergeCommandArguments { get; set; }
    }
}
