using AutoEncodeClient.ViewModels.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace AutoEncodeClient.Views
{
    /// <summary>
    /// Interaction logic for AutoEncodeClient.xaml
    /// </summary>
    public partial class AutoEncodeClientView : Window
    {
        public AutoEncodeClientView(IAutoEncodeClientViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }
}
