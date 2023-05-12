using AutoEncodeClient.Collections;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Interfaces;

namespace AutoEncodeClient.Models.StreamDataModels
{
    public class SourceStreamDataClientModel :
        ModelBase,
        IUpdateable<ISourceStreamData>
    {
        public SourceStreamDataClientModel(ISourceStreamData sourceStreamData)
        {
            DurationInSeconds = sourceStreamData.DurationInSeconds;
            NumberOfFrames = sourceStreamData.NumberOfFrames;
            VideoStream = new(sourceStreamData.VideoStream);
            AudioStreams = new BulkObservableCollection<AudioStreamData>(sourceStreamData.AudioStreams);
            if (sourceStreamData.SubtitleStreams is not null) SubtitleStreams = new BulkObservableCollection<SubtitleStreamData>(sourceStreamData.SubtitleStreams);
        }

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
        public VideoStreamDataClientModel VideoStream { get; set; }
        public BulkObservableCollection<AudioStreamData> AudioStreams { get; set; }
        public BulkObservableCollection<SubtitleStreamData> SubtitleStreams { get; set; }

        public void Update(ISourceStreamData sourceStreamData)
        {
            DurationInSeconds = sourceStreamData.DurationInSeconds;
            NumberOfFrames = sourceStreamData.NumberOfFrames;
            VideoStream.Update(sourceStreamData.VideoStream);
            AudioStreams.Update(sourceStreamData.AudioStreams);
            SubtitleStreams?.Update(sourceStreamData.SubtitleStreams);

            OnPropertyChanged(nameof(VideoStream));
            OnPropertyChanged(nameof(AudioStreams));
            OnPropertyChanged(nameof(SubtitleStreams));
        }
    }
}
