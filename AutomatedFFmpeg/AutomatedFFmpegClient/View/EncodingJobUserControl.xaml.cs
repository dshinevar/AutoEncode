using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AutomatedFFmpegUtilities.Data;

namespace AutomatedFFmpegClient.View
{
    /// <summary>
    /// Interaction logic for EncodingJobUserControl.xaml
    /// </summary>
    public partial class EncodingJobUserControl : UserControl
    {
        public EncodingJobUserControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty JobProperty =
            DependencyProperty.Register("EncodingJob", typeof(EncodingJobClientData), typeof(EncodingJobUserControl));
        public EncodingJobClientData EncodingJob
        {
            get { return (EncodingJobClientData)GetValue(JobProperty); }
            set { SetValue(JobProperty, value); }
        }
    }
}
