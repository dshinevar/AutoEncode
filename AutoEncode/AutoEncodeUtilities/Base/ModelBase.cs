using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoEncodeUtilities.Base;

public abstract class ModelBase : INotifyPropertyChanged
{
    protected virtual void SetAndNotify<U>(U oldValue, U newValue, Action setter, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<U>.Default.Equals(oldValue, newValue)) return;
        setter();
        OnPropertyChanged(propertyName);
    }

    #region Property Changed
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion Property Changed
}
