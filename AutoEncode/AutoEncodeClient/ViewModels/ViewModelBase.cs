using AutoEncodeClient.Command;
using AutoEncodeClient.Dialogs;
using AutoEncodeClient.ViewModels.Interfaces;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace AutoEncodeClient.ViewModels;

/// <summary>ViewModelBase for when functionality is needed but not a backing model.</summary>
public abstract class ViewModelBase : IViewModel
{
    private Dictionary<string, List<IAECommand>> Commands = null;

    #region Commands
    protected void AddCommand(IAECommand command, string propertyName) => AddCommand(command, [propertyName]);

    protected void AddCommand(IAECommand command, IEnumerable<string> propertyNames)
    {
        Commands ??= [];
        foreach (string propertyName in propertyNames)
        {
            if (Commands.ContainsKey(propertyName) is false)
            {
                Commands[propertyName] = [];
            }
            Commands[propertyName].Add(command);
        }
    }
    #endregion Commands

    #region Property Changed
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        if (Commands is not null)
        {
            Commands.TryGetValue(propertyName, out List<IAECommand> commands);
            commands?.ForEach(x => x?.RaiseCanExecuteChanged());
        }
    }

    private void NotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
        => OnPropertyChanged(e.PropertyName);

    protected virtual void SetAndNotify<U>(U oldValue, U newValue, Action setter, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<U>.Default.Equals(oldValue, newValue)) return;
        setter();
        OnPropertyChanged(propertyName);
    }
    #endregion Property Changed

    #region Child ViewModels
    protected void RegisterChildViewModel(IViewModel childViewModel)
    {
        childViewModel.PropertyChanged += NotifyPropertyChanged;
        childViewModel.UserMessageDialogRequested += ChildViewModel_UserMessageDialogRequested;
    }

    private void ChildViewModel_UserMessageDialogRequested(object sender, UserMessageDialogRequestedEventArgs e)
        => NotifyUserMessageDialogRequested(e);
    #endregion Child ViewModels

    #region Dialogs
    public event EventHandler<UserMessageDialogRequestedEventArgs> UserMessageDialogRequested;

    private void NotifyUserMessageDialogRequested(UserMessageDialogRequestedEventArgs e)
        => UserMessageDialogRequested?.Invoke(this, e);
    
    protected UserMessageDialogResult ShowUserMessageDialog(string message, string title, UserMessageDialogButtons buttons, Severity severity)
    {
        UserMessageDialogRequestedEventArgs eventArgs = new()
        {
            UserMessage = message,
            Title = title,
            Buttons = buttons,
            Severity = severity
        };

        UserMessageDialogRequested?.Invoke(this, eventArgs);

        return eventArgs.Result;
    }

    protected UserMessageDialogResult ShowInfoDialog(string message, string title, UserMessageDialogButtons buttons = UserMessageDialogButtons.Ok)
        => ShowUserMessageDialog(message, title, buttons, Severity.INFO);

    protected UserMessageDialogResult ShowWarningDialog(string message, string title, UserMessageDialogButtons buttons = UserMessageDialogButtons.Ok)
        => ShowUserMessageDialog(message, title, buttons, Severity.WARNING);

    protected UserMessageDialogResult ShowErrorDialog(string message, string title, UserMessageDialogButtons buttons = UserMessageDialogButtons.Ok)
        => ShowUserMessageDialog(message, title, buttons, Severity.ERROR);
    #endregion Dialogs
}

/// <summary>ViewModelBase for when a backing model is used.</summary>
/// <typeparam name="T">Model Type</typeparam>
public abstract class ViewModelBase<TModel> : ViewModelBase where TModel : class
{
    private TModel _model;
    public TModel Model
    {
        get => _model;
        protected set
        {
            if (value != _model)
            {
                TModel oldModel = _model;
                _model = value;
                ModelChange(oldModel, _model);
            }
        }
    }

    protected ViewModelBase(TModel model)
    {
        Model = model;
        if (Model is INotifyPropertyChanged notify)
        {
            notify.PropertyChanged += ModelPropertyChanged;
        }       
    }

    protected virtual void ModelChange(TModel oldModel, TModel newModel)
    {
        if (oldModel is INotifyPropertyChanged oldNotifyModel)
            oldNotifyModel.PropertyChanged -= ModelPropertyChanged;

        if (newModel is INotifyPropertyChanged newNotifyModel)
            newNotifyModel.PropertyChanged += ModelPropertyChanged;
    }

    protected virtual void ModelPropertyChanged(object sender, PropertyChangedEventArgs e) 
        => Application.Current?.Dispatcher?.BeginInvoke(() => OnPropertyChanged(e.PropertyName));
}
