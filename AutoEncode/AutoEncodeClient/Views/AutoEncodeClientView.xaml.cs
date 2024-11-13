using AutoEncodeClient.Dialogs;
using AutoEncodeClient.ViewModels.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace AutoEncodeClient.Views;

/// <summary>
/// Interaction logic for AutoEncodeClient.xaml
/// </summary>
public partial class AutoEncodeClientView : Window
{
    public AutoEncodeClientView(IAutoEncodeClientViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.UserMessageDialogRequested += ViewModel_UserMessageDialogRequested;
    }

    private void ViewModel_UserMessageDialogRequested(object sender, UserMessageDialogRequestedEventArgs e)
    {
        e.Result = UserMessageDialogHandler.ShowDialog(e.UserMessage, e.Title, e.Buttons, e.Severity, this);
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }

    private void Window_Closed(object sender, System.EventArgs e)
    {
        // Nothing to do here for now
    }
}
