// Purpose: Top-level ViewModel for the Linearity QA test tab.
//
// Responsibilities are deliberately minimal here — this class just creates
// and owns the list of EnergyConfigViewModels (one per beam energy).
// All per-energy logic lives in EnergyConfigViewModel.

using System.Collections.ObjectModel;
using Linac_QA_Software.Helpers;

namespace Linac_QA_Software.ViewModels
{
    public class LinearityViewModel : ObservableObject
    {
        /// <summary>
        /// One entry per beam energy tested (e.g. 6MV, 10MV, 6FFF).
        /// Each is rendered as a separate section or tab in the LinearityView.
        /// </summary>
        public ObservableCollection<EnergyConfigViewModel> EnergyConfigs { get; }

        public LinearityViewModel()
        {
            // Standard MU settings for a photon linearity test.
            // Adjust these if your clinic's protocol uses different values.
            int[] photonMUs = { 5, 10, 50, 100, 200, 400, 900 };

            EnergyConfigs = new ObservableCollection<EnergyConfigViewModel>
            {
                new EnergyConfigViewModel("6MV",   photonMUs),
                new EnergyConfigViewModel("10MV",  photonMUs),
                new EnergyConfigViewModel("6FFF",  photonMUs),
            };
        }
    }
}