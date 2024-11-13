using AutoEncodeUtilities.Enums;
using System.Windows;

namespace AutoEncodeClient.Dialogs;

public static class UserMessageDialogHandler
{
    public static UserMessageDialogResult ShowDialog(string message, string title, UserMessageDialogButtons buttons, Severity severity, Window owner)
    {
        UserMessageDialog dialog = new(message, title, buttons, severity, owner);
        return dialog.ShowDialog();
    }
}
