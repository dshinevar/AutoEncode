using System;
using System.Windows;
using System.Windows.Input;

namespace AutoEncodeClient.Command
{
    public interface IAECommand : ICommand
    {
        void RaiseCanExecuteChanged();
    }

    public class AECommand(Func<bool> canExecute, Action execute) : IAECommand
    {
        private readonly Func<bool> _canExecute = canExecute;
        private readonly Action _execute = execute;

        public event EventHandler CanExecuteChanged;

        public AECommand(Action execute)
            : this(() => true, execute) { }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, new EventArgs());

        public bool CanExecute(object parameter) => _canExecute();

        public void Execute(object parameter) => _execute();
    }

    public class AECommandWithParameter(Func<bool> canExecute, Action<object> execute) : IAECommand
    {
        private readonly Func<bool> _canExecute = canExecute;
        private readonly Action<object> _execute = execute;

        public event EventHandler CanExecuteChanged;

        public AECommandWithParameter(Action<object> execute)
            : this(() => true, execute) { }

        public void RaiseCanExecuteChanged() => Application.Current.Dispatcher.InvokeAsync(() => CanExecuteChanged?.Invoke(this, new EventArgs()));

        public bool CanExecute(object parameter) => _canExecute();

        public void Execute(object parameter) => _execute(parameter);
    }
}
