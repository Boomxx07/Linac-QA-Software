// Purpose: Top-level ViewModel for the Output Factor QA test tab.
//
// Responsibilities are deliberately minimal here — this class just creates
// and owns the list of OutputFactorEnergyConfigViewModels (one per beam energy).
// All per-energy logic lives in OutputFactorEnergyConfigViewModel.

using System.Collections.ObjectModel;
using Linac_QA_Software.Helpers;

namespace Linac_QA_Software.ViewModels
{
    public class OutputFactorViewModel : ObservableObject
    {
        /// <summary>
        /// One entry per beam energy tested (e.g. 6MV, 10MV, 6FFF).
        /// Each is rendered as a separate section in the OutputFactorView.
        /// </summary>
        public ObservableCollection<OutputFactorEnergyConfigViewModel> EnergyConfigs { get; }

        public OutputFactorViewModel()
        {
            EnergyConfigs = new ObservableCollection<OutputFactorEnergyConfigViewModel>
            {
                new OutputFactorEnergyConfigViewModel("6MV"),
                new OutputFactorEnergyConfigViewModel("10MV"),
                new OutputFactorEnergyConfigViewModel("6FFF"),
            };
        }
    }
}
