using AutoEncodeClient.Enums;
using AutoEncodeUtilities.Enums;

namespace AutoEncodeClient.Dialogs;

internal static class AEDialogHandler
{
    public static AEDialogButtonResult ShowDialog(string message, string title, Severity severity, AEDialogButtons buttons)
    {
        AEDialog dialog = new(message, title, severity, buttons);
        dialog.ShowDialog();

        return dialog.DialogResult;
    }

    public static AEDialogButtonResult ShowInfo(string message, string title) => ShowDialog(message, title, Severity.INFO, AEDialogButtons.Ok);

    public static AEDialogButtonResult ShowError(string message, string title) => ShowDialog(message, title, Severity.ERROR, AEDialogButtons.Ok);
}
