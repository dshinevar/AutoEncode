using AutoEncodeClient.Collections;
using AutoEncodeClient.ViewModels.EncodingJob.Interfaces;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using System.Collections.Generic;
using System.Linq;

namespace AutoEncodeClient.ViewModels.EncodingJob;

public class SourceStreamDataViewModel :
    ViewModelBase,
    ISourceStreamDataViewModel
{
    #region Properties
    private int _durationInSeconds;
    public int DurationInSeconds
    {
        get => _durationInSeconds;
        set => SetAndNotify(_durationInSeconds, value, () => _durationInSeconds = value);
    }

    private int _numberOfFrames;
    public int NumberOfFrames
    {
        get => _numberOfFrames;
        set => SetAndNotify(_numberOfFrames, value, () => _numberOfFrames = value);
    }

    private IVideoStreamDataViewModel _videoStream;
    public IVideoStreamDataViewModel VideoStream
    {
        get => _videoStream;
        set => SetAndNotify(_videoStream, value, () => _videoStream = value);
    }

    public BulkObservableCollection<AudioStreamData> AudioStreams { get; } = [];
    public IReadOnlyCollection<AudioStreamData> AudioStreamsCollection => AudioStreams;

    public BulkObservableCollection<SubtitleStreamData> SubtitleStreams { get; } = [];
    public IReadOnlyCollection<SubtitleStreamData> SubtitleStreamsCollection => SubtitleStreams;

    #endregion Properties

    public SourceStreamDataViewModel(SourceStreamData sourceStreamData)
    {
        sourceStreamData.CopyProperties(this);

        if (sourceStreamData.VideoStream is not null)
        {
            VideoStream = new VideoStreamDataViewModel(sourceStreamData.VideoStream);
            RegisterChildViewModel(VideoStream);
        }

        if (sourceStreamData.AudioStreams?.Any() ?? false)
        {
            AudioStreams.AddRange(sourceStreamData.AudioStreams);
        }

        if (sourceStreamData?.SubtitleStreams?.Any() ?? false)
        {
            SubtitleStreams.AddRange(sourceStreamData.SubtitleStreams);
        }
    }

    public void Update(SourceStreamData sourceStreamData)
    {
        sourceStreamData.CopyProperties(this);

        if (sourceStreamData.VideoStream is not null)
        {
            if (VideoStream is not null)
            {
                VideoStream = new VideoStreamDataViewModel(sourceStreamData.VideoStream);
                RegisterChildViewModel(VideoStream);
            }
            else
            {
                VideoStream.Update(sourceStreamData.VideoStream);
            }
        }

        // Not sure if worth updating at this point
        if (sourceStreamData.AudioStreams?.Any() ?? false)
        {
            AudioStreams.Update(sourceStreamData.AudioStreams);
        }

        if (sourceStreamData?.SubtitleStreams?.Any() ?? false)
        {
            SubtitleStreams.Update(sourceStreamData.SubtitleStreams);
        }
    }
}
