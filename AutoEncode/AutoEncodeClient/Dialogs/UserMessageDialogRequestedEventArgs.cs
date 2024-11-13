using AutoEncodeUtilities.Enums;
using System;

namespace AutoEncodeClient.Dialogs;

public class UserMessageDialogRequestedEventArgs : EventArgs
{
    public string UserMessage { get; set; }

    public string Title { get; set; }

    public UserMessageDialogButtons Buttons { get; set; }

    public Severity Severity { get; set; }

    public UserMessageDialogResult Result { get; set; }
}
