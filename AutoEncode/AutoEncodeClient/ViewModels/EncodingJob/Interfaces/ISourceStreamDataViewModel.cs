using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Interfaces;
using System.Collections.Generic;

namespace AutoEncodeClient.ViewModels.EncodingJob.Interfaces;

public interface ISourceStreamDataViewModel :
    IViewModel,
    IUpdateable<SourceStreamData>
{
    int DurationInSeconds { get; }

    int NumberOfFrames { get; }

    IVideoStreamDataViewModel VideoStream { get; }

    IReadOnlyCollection<AudioStreamData> AudioStreamsCollection { get; }

    IReadOnlyCollection<SubtitleStreamData> SubtitleStreamsCollection { get; }
}
