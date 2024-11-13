using AutoEncodeClient.Command;
using AutoEncodeUtilities.Enums;
using System.Windows;
using System.Windows.Input;

namespace AutoEncodeClient.Dialogs;

/// <summary>
/// Interaction logic for UserMessageDialog.xaml
/// </summary>
public partial class UserMessageDialog : Window
{
    public string Message { get; }
    public Severity Severity { get; }
    public UserMessageDialogButtons Buttons { get; }
    public new UserMessageDialogResult DialogResult { get; set; }

    public ICommand ButtonResultCommand { get; }


    public UserMessageDialog(string message, string title, UserMessageDialogButtons buttons, Severity severity, Window owner)
    {
        InitializeComponent();

        ButtonResultCommand = new AECommand(ButtonResult);

        Message = message;
        Title = title;
        Severity = severity;
        Buttons = buttons;

        Owner = owner;

        DataContext = this;
    }

    public new UserMessageDialogResult ShowDialog()
    {
        base.ShowDialog();
        return DialogResult;
    }

    private void ButtonResult(object obj)
    {
        if (obj is UserMessageDialogResult result)
        {
            DialogResult = result;
        }

        Close();
    }
}
