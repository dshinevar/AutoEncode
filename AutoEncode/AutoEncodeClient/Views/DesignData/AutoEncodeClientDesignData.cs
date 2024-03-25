using AutoEncodeClient.Collections;
using AutoEncodeClient.Models;
using AutoEncodeClient.Models.Interfaces;
using AutoEncodeClient.ViewModels;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace AutoEncodeClient.Views.DesignData
{
    public class AutoEncodeClientDesignData : IAutoEncodeClientViewModel
    {
        public AutoEncodeClientDesignData()
        {
            EncodingJobs = [];
            IEncodingJobData encodingJobData1 = new EncodingJobData()
            {
                Id = 1,
                SourceFullPath = "M:\\Movies\\Very Evil Alive (2025).mkv",
                DestinationFullPath = "M:\\Movies (Encoded)\\Very Evil Alive (2025).mkv",
                Status = EncodingJobStatus.ENCODING,
                EncodingProgress = 50
            };
            IEncodingJobData encodingJobData2 = new EncodingJobData()
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
                new List<AudioStreamData>()
                {
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
                },
                new List<SubtitleStreamData>()
                {
                    new()
                    {
                        StreamIndex = 3,
                        Title = "Subtitle",
                        SubtitleIndex = 0,
                        Forced = true,
                        Language = "eng",
                        Descriptor = "Subtitle Descriptor"
                    }
                }),
                EncodingCommandArguments = new DolbyVisionEncodingCommandArguments()
                {
                    VideoEncodingCommandArguments = "Video Encoding Args",
                    AudioSubsEncodingCommandArguments = "AudioSub Encoding Args",
                    MergeCommandArguments = "Merge Args"
                }
            };
            IEncodingJobData encodingJobData4 = new EncodingJobData()
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
            IEncodingJobData encodingJobData3 = new EncodingJobData()
            {
                Id = 3,
                SourceFullPath = "M:\\Movies\\Knifin Around (1984).mkv",
                DestinationFullPath = "M:\\Movies (Encoded)\\Knifin Around (1984).mkv",
                Status = EncodingJobStatus.NEW,
                EncodingProgress = 0
            };

            var encodingJobClientModel1 = new EncodingJobClientModel(encodingJobData1);
            var encodingJobClientModel2 = new EncodingJobClientModel(encodingJobData2);
            var encodingJobClientModel4 = new EncodingJobClientModel(encodingJobData4);
            var encodingJobClientModel3 = new EncodingJobClientModel(encodingJobData3);

            var encodingJobViewModel1 = new EncodingJobViewModel(encodingJobClientModel1);
            var encodingJobViewModel2 = new EncodingJobViewModel(encodingJobClientModel2)
            {
                SelectedDetailsSection = Enums.EncodingJobDetailsSection.Source_Data
            };
            var encodingJobViewModel4 = new EncodingJobViewModel(encodingJobClientModel4);
            var encodingJobViewModel3 = new EncodingJobViewModel(encodingJobClientModel3);


            EncodingJobs.Add(encodingJobViewModel1);
            EncodingJobs.Add(encodingJobViewModel2);
            EncodingJobs.Add(encodingJobViewModel4);
            EncodingJobs.Add(encodingJobViewModel3);

            SelectedEncodingJobViewModel = encodingJobViewModel1;

            MovieSourceFiles = new ObservableDictionary<string, BulkObservableCollection<SourceFileData>>()
            {
                {   "Movies",
                    new BulkObservableCollection<SourceFileData>()
                    {
                        new() { FullPath = "C:\\Movies\\Knifin Around (1984).mkv", Encoded = true },
                        new() { FullPath = "C:\\Movies\\Halloween - Michael's Birthday Party (2030).mkv", Encoded = false }
                    }
                },
                {
                    "Kids Movies",
                    new BulkObservableCollection<SourceFileData>()
                    {
                        new() { FullPath = "C:\\Kids Movies\\Paddington 2077 (2077).mkv", Encoded = true}
                    }
                }
            };

            ShowSourceFiles = [];
        }

        #region Properties
        public ISourceFilesViewModel SourceFilesViewModel { get; set; } = new SourceFilesViewModel()
        {
            MovieSourceFiles = new ObservableDictionary<string, IEnumerable<SourceFileData>>()
            {
                {   "Movies",
                    new List<SourceFileData>()
                    {
                        new() { FullPath = "C:\\Movies\\Knifin Around (1984).mkv", Encoded = true },
                        new() { FullPath = "C:\\Movies\\Halloween - Michael's Birthday Party (2030).mkv", Encoded = false }
                    }
                },
                {
                    "Kids Movies",
                    new List<SourceFileData>()
                    {
                        new() { FullPath = "C:\\Kids Movies\\Paddington 2077 (2077).mkv", Encoded = true}
                    }
                }
            },
            ShowSourceFiles = new ObservableDictionary<string, IEnumerable<ShowSourceFileViewModel>>()
            {
                {   "TV Shows",
                    new List<ShowSourceFileViewModel>()
                    {
                        new() { ShowName = "Metalocalypse", Seasons =
                            [
                                new() { SeasonInt = 1, Episodes =
                                    [
                                        new() { FullPath = "C:\\TV Shows\\Metalocalypse\\Season 1\\Metalocalypse - s01e01 - Death Metal" }
                                    ]
                                }
                            ]
                        }
                    }
                }
            }
        };
        public ICommand RefreshSourceFilesCommand { get; }
        public BulkObservableCollection<IEncodingJobViewModel> EncodingJobs { get; }
        public IEncodingJobViewModel SelectedEncodingJobViewModel { get; set; }
        public ObservableDictionary<string, BulkObservableCollection<SourceFileData>> MovieSourceFiles { get; set; }
        public ObservableDictionary<string, ObservableDictionary<string, ObservableDictionary<string, BulkObservableCollection<ShowSourceFileData>>>> ShowSourceFiles { get; set; }

        public bool ConnectedToServer { get; set; } = true;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Initialize(IAutoEncodeClientModel model) => throw new NotImplementedException();
        #endregion Properties
    }
}
