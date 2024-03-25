using AutoEncodeClient.Collections;
using AutoEncodeClient.Models.StreamDataModels;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Interfaces;
using System.ComponentModel;

namespace AutoEncodeClient.Models.Interfaces
{
    public interface ISourceStreamDataClientModel :
        IUpdateable<ISourceStreamData>,
        INotifyPropertyChanged
    {
        int DurationInSeconds { get; }

        int NumberOfFrames { get; }

        VideoStreamDataClientModel VideoStream { get; }

        BulkObservableCollection<AudioStreamData> AudioStreams { get; }

        BulkObservableCollection<SubtitleStreamData> SubtitleStreams { get; }
    }
}
