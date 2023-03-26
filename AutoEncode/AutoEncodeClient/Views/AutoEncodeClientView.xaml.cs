using AutoEncodeClient.Interfaces;
using AutoEncodeClient.ViewModels;
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

namespace AutoEncodeClient.Views
{
    /// <summary>
    /// Interaction logic for AutoEncodeClient.xaml
    /// </summary>
    public partial class AutoEncodeClientView : Window
    {
        public AutoEncodeClientView(IAutoEncodeClientViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
