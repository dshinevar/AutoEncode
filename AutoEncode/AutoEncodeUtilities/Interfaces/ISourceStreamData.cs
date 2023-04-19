using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Interfaces
{
    public interface ISourceStreamData
    {
        int DurationInSeconds { get; set; }
        /// <summary>This is an approx. number; Used for dolby vision jobs</summary>
        int NumberOfFrames { get; set; }
        VideoStreamData VideoStream { get; set; }
        List<AudioStreamData> AudioStreams { get; set; }
        List<SubtitleStreamData> SubtitleStreams { get; set; }
    }
}
