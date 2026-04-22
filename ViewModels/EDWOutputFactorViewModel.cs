// Purpose: Top-level ViewModel for the EDW Output Factor QA test tab.
//
// Responsibilities: Creates and owns the list of EDWOutputFactorEnergyConfigViewModels 
// (one per beam energy). All per-energy logic lives in the child ViewModel.

using System.Collections.ObjectModel;
using Linac_QA_Software.Helpers;

namespace Linac_QA_Software.ViewModels
{
    public class EDWOutputFactorViewModel : ObservableObject
    {
        /// <summary>
        /// One entry per beam energy tested (e.g. 6MV, 10MV).
        /// Each is rendered as a separate section in the EDWOutputFactorView.
        /// </summary>
        public ObservableCollection<EDWOutputFactorEnergyConfigViewModel> EnergyConfigs { get; }

        public EDWOutputFactorViewModel()
        {
            EnergyConfigs = new ObservableCollection<EDWOutputFactorEnergyConfigViewModel>
            {
                new EDWOutputFactorEnergyConfigViewModel("6MV"),
                new EDWOutputFactorEnergyConfigViewModel("10MV")
            };
        }
    }
}