using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeUtilities.Interfaces;

public interface ISourceStreamData
{
    int DurationInSeconds { get; }

    /// <summary>This is an approx. number; Used for dolby vision jobs</summary>
    int NumberOfFrames { get; }

    VideoStreamData VideoStream { get; }

    IEnumerable<AudioStreamData> AudioStreams { get; }

    IEnumerable<SubtitleStreamData> SubtitleStreams { get; }
}
