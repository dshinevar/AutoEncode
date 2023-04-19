using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoEncodeClient.Command
{
    public class AECommand : ICommand
    {
        private readonly Func<bool> _canExecute;
        private readonly Action _execute;

        public event EventHandler CanExecuteChanged;

        public AECommand(Func<bool> canExecute, Action execute ) 
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        public AECommand(Action execute)
            : this(() => true, execute) { }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, new EventArgs());

        public bool CanExecute(object parameter) => _canExecute();

        public void Execute(object parameter) => _execute();
    }
}
