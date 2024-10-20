﻿using AutoEncodeClient.Command;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace AutoEncodeClient.ViewModels;

/// <summary>ViewModelBase for when functionality is needed but not a backing model.</summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    private Dictionary<string, List<IAECommand>> Commands = null;

    protected void AddCommand(IAECommand command, string propertyName) => AddCommand(command, new string[] { propertyName });

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

    protected virtual void SetAndNotify<U>(U oldValue, U newValue, Action setter, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<U>.Default.Equals(oldValue, newValue)) return;
        setter();
        OnPropertyChanged(propertyName);
    }

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
    #endregion Property Changed
}

/// <summary>ViewModelBase for when a backing model is used.</summary>
/// <typeparam name="T">Model Type</typeparam>
public abstract class ViewModelBase<T> : ViewModelBase where T : INotifyPropertyChanged
{
    public T Model { get; protected set; }

    protected ViewModelBase() { }

    protected ViewModelBase(T model)
    {
        Model = model;
        Model.PropertyChanged += ModelPropertyChanged;
    }

    protected virtual void ModelPropertyChanged(object sender, PropertyChangedEventArgs e) => Application.Current.Dispatcher.Invoke(() => OnPropertyChanged(e.PropertyName));
}
