using System;
using System.Windows.Input;

namespace AutoEncodeClient.Command;

public interface IAECommand : ICommand
{
    void RaiseCanExecuteChanged();
}

public class AECommand(Func<object, bool> canExecute, Action<object> execute) : IAECommand
{
    private readonly Func<object, bool> _canExecute = canExecute;
    private readonly Action<object> _execute = execute;

    public event EventHandler CanExecuteChanged;

    public AECommand(Func<object, bool> canExecute, Action execute)
        : this(canExecute, obj => execute()) { }

    public AECommand(Func<bool> canExecute, Action<object> execute)
        : this(obj => canExecute(), execute) { }

    public AECommand(Func<bool> canExecute, Action execute)
        : this(obj => canExecute(), obj => execute()) { }

    public AECommand(Action<object> execute)
        : this(obj => true, execute) { }

    public AECommand(Action execute)
        : this(() => true, execute) { }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, new EventArgs());

    public bool CanExecute(object parameter) => _canExecute(parameter);

    public void Execute(object parameter) => _execute(parameter);
}
