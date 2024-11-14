using AutoEncodeClient.Enums;
using AutoEncodeClient.Models;
using AutoEncodeClient.ViewModels;
using AutoEncodeClient.ViewModels.EncodingJob;
using AutoEncodeClient.ViewModels.EncodingJob.Interfaces;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace AutoEncodeClient.Views.DesignData;

public class EncodingJobsViewDesignData : ViewModelBase, IEncodingJobQueueViewModel
{
    public EncodingJobsViewDesignData()
    {
        EncodingJobData encodingJobData1 = new()
        {
            Id = 1,
            SourceFullPath = "M:\\Movies\\Very Evil Alive (2025).mkv",
            DestinationFullPath = "M:\\Movies (Encoded)\\Very Evil Alive (2025).mkv",
            Status = EncodingJobStatus.ENCODING,
            EncodingProgress = 50
        };
        EncodingJobData encodingJobData2 = new()
        {
            Id = 2,
            SourceFullPath = "M:\\Movies\\Big Big Big Little Big Explosion (2050).mkv",
            DestinationFullPath = "M:\\Movies (Encoded)\\Big Big Big Little Big Explosion (2050).mkv",
            Status = EncodingJobStatus.ENCODED,
            EncodingProgress = 0,
            ElapsedEncodingTime = new TimeSpan(1, 2, 23, 56),
            CompletedEncodingDateTime = DateTime.Now,
            Complete = true,
            HasError = false,
            ErrorMessage = "A really, really, bad thing happened. There was a fire. Everything burnt to the ground.",
            ErrorTime = DateTime.Now,
            SourceStreamData = new SourceStreamData(123456789, 987654,
            new VideoStreamData()
            {
                StreamIndex = 0,
                Title = "Video",
                ScanType = VideoScanType.INTERLACED_TFF,
                ChromaLocation = ChromaLocation.LEFT_DEFAULT,
                PixelFormat = "yuv420p10le",
                CodecName = "hevc",
                ColorPrimaries = "bt2020",
                ColorSpace = "bt2020nc",
                ColorTransfer = "smpte2084",
                Resolution = "3842x2160",
                Crop = "200:200:200:300",
                FrameRate = "24000/1001",

                HDRData = new HDRData()
                {
                    HDRFlags = HDRFlags.HDR10 | HDRFlags.DOLBY_VISION,
                    Red_X = "100",
                    Red_Y = "1000000"
                }
            },
            [
                new()
                    {
                        StreamIndex = 1,
                        Title = "Audio",
                        AudioIndex = 0,
                        Channels = 6,
                        Commentary = false,
                        Language = "eng",
                        CodecName = "dts-hd",
                        ChannelLayout = "5.1",
                        Descriptor = "Audio Descriptor"
                    },
                    new()
                    {
                        StreamIndex = 2,
                        Title = "Audio",
                        AudioIndex = 1,
                        Channels = 2,
                        Commentary = false,
                        Language = "jn",
                        CodecName = "dts",
                        ChannelLayout = "5",
                        Descriptor = "Audio Descriptor"
                    }
            ],
            [
                new()
                    {
                        StreamIndex = 3,
                        Title = "Subtitle",
                        SubtitleIndex = 0,
                        Forced = true,
                        Language = "eng",
                        Descriptor = "Subtitle Descriptor"
                    }
            ]),
            EncodingCommandArguments = new EncodingCommandArguments(true)
        };
        EncodingJobData encodingJobData4 = new()
        {
            Id = 4,
            SourceFullPath = "M:\\Movies\\Halloween - Michael's Birthday Party (2030).mkv",
            DestinationFullPath = "M:\\Movies (Encoded)\\Halloween - Michael's Birthday Party (2030).mkv",
            Status = EncodingJobStatus.BUILDING,
            EncodingProgress = 0,
            HasError = true,
            ErrorMessage = "ffmpeg has exploded into 13 pieces.",
            ErrorTime = DateTime.Now
        };
        EncodingJobData encodingJobData3 = new()
        {
            Id = 3,
            SourceFullPath = "M:\\Movies\\Knifin Around (1984).mkv",
            DestinationFullPath = "M:\\Movies (Encoded)\\Knifin Around (1984).mkv",
            Status = EncodingJobStatus.BUILDING,
            EncodingProgress = 0
        };

        var encodingJobClientModel1 = new EncodingJobClientModel(encodingJobData1);
        var encodingJobClientModel2 = new EncodingJobClientModel(encodingJobData2);
        var encodingJobClientModel4 = new EncodingJobClientModel(encodingJobData4);
        var encodingJobClientModel3 = new EncodingJobClientModel(encodingJobData3);

        var encodingJobViewModel1 = new EncodingJobViewModel(encodingJobClientModel1)
        {
            SelectedDetailsSection = EncodingJobDetailsSection.Source_Data
        };
        var encodingJobViewModel2 = new EncodingJobViewModel(encodingJobClientModel2)
        {
            SelectedDetailsSection = EncodingJobDetailsSection.Source_Data
        };
        var encodingJobViewModel4 = new EncodingJobViewModel(encodingJobClientModel4);
        var encodingJobViewModel3 = new EncodingJobViewModel(encodingJobClientModel3);


        EncodingJobs.Add(encodingJobViewModel1);
        EncodingJobs.Add(encodingJobViewModel2);
        EncodingJobs.Add(encodingJobViewModel4);
        EncodingJobs.Add(encodingJobViewModel3);

        SelectedEncodingJobViewModel = encodingJobViewModel2;

        EncodingJobsView = CollectionViewSource.GetDefaultView(EncodingJobs);
    }

    private readonly ObservableCollection<IEncodingJobViewModel> EncodingJobs = [];

    public IEncodingJobViewModel SelectedEncodingJobViewModel { get; set; }

    public ICollectionView EncodingJobsView { get; set; }

    public void Initialize()
    {
        throw new NotImplementedException();
    }

    public void Shutdown()
    {
        throw new NotImplementedException();
    }
}
