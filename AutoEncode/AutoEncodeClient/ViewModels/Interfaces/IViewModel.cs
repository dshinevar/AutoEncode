using AutoEncodeClient.Dialogs;
using System;
using System.ComponentModel;

namespace AutoEncodeClient.ViewModels.Interfaces;

public interface IViewModel :
    INotifyPropertyChanged
{
    event EventHandler<UserMessageDialogRequestedEventArgs> UserMessageDialogRequested;
}
