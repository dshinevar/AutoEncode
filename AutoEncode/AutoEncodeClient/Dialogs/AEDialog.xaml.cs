using AutoEncodeClient.Command;
using AutoEncodeClient.Enums;
using AutoEncodeUtilities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

            ButtonResultCommand = new AECommandWithParameter(ButtonResult);
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
