using AutoEncodeClient.Command;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoEncodeClient.ViewModels
{
    public abstract class ViewModelBase<T> : INotifyPropertyChanged
    {
        protected T Model { get; private set; }

        private Dictionary<string, List<AECommand>> Commands = null;

        protected ViewModelBase(T model)
        {
            Model = model;
        }

        protected void AddCommand(AECommand command, string propertyName) => AddCommand(command, new string[] { propertyName });

        protected void AddCommand(AECommand command, IEnumerable<string> propertyNames)
        {
            Commands ??= new Dictionary<string, List<AECommand>>();
            foreach (string propertyName in propertyNames)
            {
                if (Commands.ContainsKey(propertyName) is false)
                {
                    Commands[propertyName] = new List<AECommand>();
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
                Commands.TryGetValue(propertyName, out List<AECommand> commands);
                commands?.ForEach(x => x?.RaiseCanExecuteChanged());
            }
        }
        #endregion Property Changed
    }
}
