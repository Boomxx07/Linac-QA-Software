// Purpose: Code-behind for the application's main window.
//
// In MVVM, code-behind files should be kept as thin as possible.
// The only responsibility here is to instantiate MainViewModel and set
// it as the DataContext so that all XAML bindings resolve correctly.

using Linac_QA_Software.ViewModels;
using System.Windows;

namespace Linac_QA_Software
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}