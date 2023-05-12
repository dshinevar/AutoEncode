using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeClient.Models;
using AutoEncodeClient.ViewModels;
using AutoEncodeClient.Collections;
using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace AutoEncodeClient.Views.DesignData
{
    public class AutoEncodeClientDesignData : IAutoEncodeClientViewModel
    {
        public AutoEncodeClientDesignData() 
        {
            EncodingJobs = new BulkObservableCollection<EncodingJobViewModel>();
            var encodingJobData1 = new EncodingJobData()
            {
                Id = 1,
                SourceFullPath = "M:\\Movies\\Very Evil Alive (2025).mkv",
                DestinationFullPath = "M:\\Movies (Encoded)\\Very Evil Alive (2025).mkv",
                Status = EncodingJobStatus.ENCODING,
                EncodingProgress = 50
            };
            var encodingJobData2 = new EncodingJobData()
            {
                Id = 2,
                SourceFullPath = "M:\\Movies\\Big Big Big Little Big Explosion (2050).mkv",
                DestinationFullPath = "M:\\Movies (Encoded)\\Big Big Big Little Big Explosion (2050).mkv",
                Status = EncodingJobStatus.BUILT,
                EncodingProgress = 0,
                ElapsedEncodingTime = new TimeSpan(1, 2, 23, 56),
                CompletedEncodingDateTime = DateTime.Now,
                Complete = true,
                Error = false,
                LastErrorMessage = "A really, really, bad thing happened. There was a fire. Everything burnt to the ground.",
                ErrorTime = DateTime.Now,
                SourceStreamData = new SourceStreamData()
                {
                    VideoStream = new VideoStreamData()
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
                    },
                    AudioStreams = new List<AudioStreamData>()
                    {
                        new AudioStreamData()
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
                        new AudioStreamData()
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
                    SubtitleStreams = new List<SubtitleStreamData>()
                    {
                        new SubtitleStreamData()
                        {
                            StreamIndex = 3,
                            Title = "Subtitle",
                            SubtitleIndex = 0,
                            Forced = true,
                            Language = "eng",
                            Descriptor = "Subtitle Descriptor"
                        }
                    },
                    DurationInSeconds = 123456789,
                    NumberOfFrames = 987654
                }
            };
            var encodingJobData4 = new EncodingJobData()
            {
                Id = 4,
                SourceFullPath = "M:\\Movies\\Halloween - Michael's Birthday Party (2030).mkv",
                DestinationFullPath = "M:\\Movies (Encoded)\\Halloween - Michael's Birthday Party (2030).mkv",
                Status = EncodingJobStatus.BUILDING,
                EncodingProgress = 0,
                Error = true
            };
            var encodingJobData3 = new EncodingJobData()
            {
                Id = 3,
                SourceFullPath = "M:\\Movies\\Knifin Around (1984).mkv",
                DestinationFullPath = "M:\\Movies (Encoded)\\Knifin Around (1984).mkv",
                Status = EncodingJobStatus.NEW,
                EncodingProgress = 0
            };

            var encodingJobClientModel1 = new EncodingJobClientModel(encodingJobData1, null);
            var encodingJobClientModel2 = new EncodingJobClientModel(encodingJobData2, null);
            var encodingJobClientModel4 = new EncodingJobClientModel(encodingJobData4, null);
            var encodingJobClientModel3 = new EncodingJobClientModel(encodingJobData3, null);

            var encodingJobViewModel1 = new EncodingJobViewModel(encodingJobClientModel1);
            var encodingJobViewModel2 = new EncodingJobViewModel(encodingJobClientModel2);
            var encodingJobViewModel4 = new EncodingJobViewModel(encodingJobClientModel4);
            var encodingJobViewModel3 = new EncodingJobViewModel(encodingJobClientModel3);

            EncodingJobs.Add(encodingJobViewModel1);
            EncodingJobs.Add(encodingJobViewModel2);
            EncodingJobs.Add(encodingJobViewModel4);
            EncodingJobs.Add(encodingJobViewModel3);

            SelectedEncodingJobViewModel = encodingJobViewModel2;

            MovieSourceFiles = new ObservableDictionary<string, BulkObservableCollection<VideoSourceData>>()
            {
                {   "Movies",
                    new BulkObservableCollection<VideoSourceData>()
                    {
                        new VideoSourceData() { FullPath = "C:\\Movies\\Knifin Around (1984).mkv", Encoded = true },
                        new VideoSourceData() { FullPath = "C:\\Movies\\Halloween - Michael's Birthday Party (2030).mkv", Encoded = false }
                    }
                },
                {
                    "Kids Movies",
                    new BulkObservableCollection<VideoSourceData>()
                    {
                        new VideoSourceData() { FullPath = "C:\\Kids Movies\\Paddington 2077 (2077).mkv", Encoded = true}
                    }
                }
            };

            ShowSourceFiles = new ObservableDictionary<string, BulkObservableCollection<ShowSourceData>>()
            {
                {   "Shows",
                    new BulkObservableCollection<ShowSourceData>()
                    {
                        new ShowSourceData() { ShowName = "Seinfeld",
                            Seasons = new List<SeasonSourceData>()
                            {
                                new SeasonSourceData() { Season = "1",
                                    Episodes = new List<VideoSourceData>
                                    {
                                        new VideoSourceData() { FullPath = "C:\\Shows\\Seinfeld\\Season 1\\Seinfeld - s01e01 - Pilot.mkv", Encoded = true },
                                        new VideoSourceData() { FullPath = "C:\\Shows\\Seinfeld\\Season 1\\Seinfeld - s01e02 - The Second Pilot.mkv", Encoded = false}
                                    }
                                },
                                new SeasonSourceData() { Season = "2",
                                    Episodes = new List<VideoSourceData>
                                    {
                                        new VideoSourceData() { FullPath = "C:\\Shows\\Seinfeld\\Season 2\\Seinfeld - s02e01 - Kramer's Revenge.mkv", Encoded = false},
                                        new VideoSourceData() { FullPath = "C:\\Shows\\Seinfeld\\Season 2\\Seinfeld - s02e03 - Summer of George.mkv", Encoded = true}
                                    }
                                }
                            }
                        },
                        new ShowSourceData() { ShowName = "Metalocalypse",
                            Seasons = new List<SeasonSourceData>()
                            {
                                new SeasonSourceData() { Season = "3",
                                    Episodes = new List<VideoSourceData>
                                    {
                                        new VideoSourceData() { FullPath = "C:\\Shows\\Metalocalypse\\Season 3\\Metalocalypse - s03e01 - Metal.mkv", Encoded = true },
                                        new VideoSourceData() { FullPath = "C:\\Shows\\Metalocalypse\\Season 3\\Metalocalypse - s03e02 - Death Metal.mkv", Encoded = false}
                                    }
                                },
                                new SeasonSourceData() { Season = "4",
                                    Episodes = new List<VideoSourceData>
                                    {
                                        new VideoSourceData() { FullPath = "C:\\Shows\\Metalocalypse\\Season 4\\Metalocalypse - s04e01 - Black Metal.mkv", Encoded = false},
                                        new VideoSourceData() { FullPath = "C:\\Shows\\Metalocalypse\\Season 4\\Metalocalypse - s04e03 - Metalcore.mkv", Encoded = true}
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        #region Properties
        public ICommand RefreshSourceFilesCommand { get; }
        public BulkObservableCollection<EncodingJobViewModel> EncodingJobs { get; }
        public EncodingJobViewModel SelectedEncodingJobViewModel { get; set; }
        public ObservableDictionary<string, BulkObservableCollection<VideoSourceData>> MovieSourceFiles { get; set; }
        public ObservableDictionary<string, BulkObservableCollection<ShowSourceData>> ShowSourceFiles { get; set; }

        public bool ConnectedToServer { get; set; } = true;

        public void Dispose()
        {
            throw new NotImplementedException();
        }
        #endregion Properties
    }
}
