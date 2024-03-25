using AutoEncodeClient.Collections;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.Models.StreamDataModels;
using AutoEncodeUtilities.Data;

namespace AutoEncodeClient.ViewModels
{
    public class SourceStreamDataViewModel :
        ViewModelBase<ISourceStreamDataClientModel>
    {
        public SourceStreamDataViewModel(ISourceStreamDataClientModel model)
        {
            Model = model;
        }

        public int DurationInSeconds => Model.DurationInSeconds;

        public int NumberOfFrames => Model.NumberOfFrames;

        public VideoStreamDataClientModel VideoStream => Model.VideoStream;

        public BulkObservableCollection<AudioStreamData> AudioStreams => Model.AudioStreams;

        public BulkObservableCollection<SubtitleStreamData> SubtitleStreams => Model.SubtitleStreams;
    }
}
