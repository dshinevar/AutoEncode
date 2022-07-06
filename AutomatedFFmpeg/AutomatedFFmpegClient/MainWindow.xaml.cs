using AutomatedFFmpegClient.Config;
using AutomatedFFmpegClient.ViewData;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Messages;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace AutomatedFFmpegClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AFClientMainThread _mainThread;

        public ObservableCollection<VideoSourceViewData> VideoSource { get; set; } = new ObservableCollection<VideoSourceViewData>();
        public ObservableCollection<ShowSourceViewData> ShowSource { get; set; } = new ObservableCollection<ShowSourceViewData>();
        public ObservableCollection<EncodingJobClientData> EncodingJobs { get; set; } = new ObservableCollection<EncodingJobClientData>();

        public MainWindow(AFClientConfig config)
        {
            InitializeComponent();
            DataContext = this;
            _mainThread = new AFClientMainThread(this, config);
            _mainThread.Start();
        }

        public void UpdateVideoSource(Dictionary<string, List<VideoSourceData>> videoSourceFiles)
        {
            Dispatcher.Invoke(() =>
            {
                VideoSource.Clear();
                foreach (KeyValuePair<string, List<VideoSourceData>> data in videoSourceFiles)
                {
                    VideoSourceViewData viewData = new VideoSourceViewData(data.Key, data.Value);
                    VideoSource.Add(viewData);
                }
            });
        }

        public void UpdateShowSource(Dictionary<string, List<ShowSourceData>> showSourceFiles)
        {
            Dispatcher.Invoke(() =>
            {
                ShowSource.Clear();
                foreach (KeyValuePair<string, List<ShowSourceData>> data in showSourceFiles)
                {
                    ShowSourceViewData viewData = new ShowSourceViewData(data.Key, data.Value);
                    ShowSource.Add(viewData);
                }
            });
        }

        public void UpdateEncodingJobs(List<EncodingJobClientData> encodingJobs)
        {
            Dispatcher.Invoke(() =>
            {
                EncodingJobs.Clear();
                foreach (EncodingJobClientData encodingJob in encodingJobs)
                {
                    EncodingJobs.Insert(0, encodingJob);
                }
            });
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            _mainThread.Connect();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            _mainThread.Send(new SourceFileRefreshMessage());
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            _mainThread.Disconnect();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _mainThread.Shutdown();
        }

        private void VideoEncode_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = videoTree.SelectedItem;
        }
    }
}
