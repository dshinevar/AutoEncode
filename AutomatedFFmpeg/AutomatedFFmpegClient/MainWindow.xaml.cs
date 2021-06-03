using AutomatedFFmpegClient.Config;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Messages;
using AutomatedFFmpegUtilities.Data;
using AutomatedFFmpegUtilities.Messages.ClientToServer;
using AutomatedFFmpegUtilities.Messages.ServerToClient;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AutomatedFFmpegClient.Model;
using System.Windows;

namespace AutomatedFFmpegClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AFClientMainThread _mainThread;

        public ObservableCollection<VideoSourceViewModel> VideoSource { get; set; } = new ObservableCollection<VideoSourceViewModel>();

        public MainWindow(AFClientConfig config)
        {
            InitializeComponent();
            DataContext = this;
            _mainThread = new AFClientMainThread(this, config);
            _mainThread.Start();
        }

        public void UpdateSource(IEnumerable<VideoSourceViewModel> models)
        {
            Dispatcher.Invoke(new System.Action(() =>
            {
                VideoSource.Clear();
                foreach (VideoSourceViewModel model in models)
                {
                    VideoSource.Add(model);
                }
            }));
        }
        

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            _mainThread.Connect();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            _mainThread.Send(new CTSTest() { MessageType = AFMessageType.CLIENT_REQUEST });
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            _mainThread.Disconnect();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _mainThread.Shutdown();
        }
    }
}
