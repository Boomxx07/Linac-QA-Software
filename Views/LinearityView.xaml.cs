// Purpose: Code-behind for the Linearity QA user control.
//
// This file is intentionally empty beyond the constructor — all logic
// lives in LinearityViewModel and its children.  The DataContext is
// inherited from the parent MainWindow via XAML binding.

using System.Windows.Controls;

namespace Linac_QA_Software.Views
{
    public partial class LinearityView : UserControl
    {
        public LinearityView()
        {
            InitializeComponent();
        }
    }
}