using Linac_QA_Software.Helpers;
using Linac_QA_Software.Models;
using System.Collections.ObjectModel;

namespace Linac_QA_Software.ViewModels
{
    /// <summary>
    /// Top-level ViewModel for the Linearity QA test tab.
    /// Owns the collection of energy-specific controllers.
    /// </summary>
    public class LinearityViewModel : ObservableObject
    {
        public ObservableCollection<LinearityEnergyViewModel> EnergyConfigs { get; }

        public LinearityViewModel()
        {
            // 1. Load configuration once at the top level
            var config = ConfigLoader.Load("config.json");
            
            // Standard MU settings for a photon linearity test.
            // Notice we use double arrays now to match the high-precision physics layer.
            double[] photonMUs = { 5, 10, 50, 100, 200, 400, 900 };

            EnergyConfigs = new ObservableCollection<LinearityEnergyViewModel>
            {
                new LinearityEnergyViewModel("6MV",   photonMUs, config),
                new LinearityEnergyViewModel("10MV",  photonMUs, config),
                new LinearityEnergyViewModel("6FFF",  photonMUs, config),
            };
        }
    }
}