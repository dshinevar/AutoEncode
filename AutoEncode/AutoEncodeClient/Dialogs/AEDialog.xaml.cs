using AutoEncodeClient.Command;
using AutoEncodeClient.Enums;
using AutoEncodeUtilities.Enums;
using System.Windows;
using System.Windows.Input;

namespace AutoEncodeClient.Dialogs
{
    /// <summary>
    /// Interaction logic for AEDialog.xaml
    /// </summary>
    public partial class AEDialog : Window
    {
        public string Message { get; }
        public Severity Severity { get; }
        public AEDialogButtons Buttons { get; }
        public new AEDialogButtonResult DialogResult { get; set; }

        public ICommand ButtonResultCommand { get; }


        public AEDialog(string message, string title, Severity severity, AEDialogButtons buttons)
        {
            InitializeComponent();
            DataContext = this;

            Message = message;
            Title = title;
            Severity = severity;
            Buttons = buttons;

            ButtonResultCommand = new AECommand(ButtonResult);
        }

        private void ButtonResult(object obj)
        {
            if (obj is AEDialogButtonResult result)
            {
                DialogResult = result;
                Close();
            }
        }
    }
}
