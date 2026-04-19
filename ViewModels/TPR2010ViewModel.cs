// Purpose: Top-level ViewModel for the TPR2010 QA test tab.
//
// Owns the collection of TPR2010EnergyRowViewModels (one per beam energy).
// All logic is delegated to the energy-level ViewModels.

using System.Collections.ObjectModel;
using Linac_QA_Software.Helpers;

namespace Linac_QA_Software.ViewModels
{
    public class TPR2010ViewModel : ObservableObject
    {
        /// <summary>
        /// One entry per beam energy tested (6MV, 10MV, 6FFF).
        /// All are displayed in a single consolidated table.
        /// </summary>
        public ObservableCollection<TPR2010EnergyRowViewModel> EnergyRows { get; }

        public TPR2010ViewModel()
        {
            EnergyRows = new ObservableCollection<TPR2010EnergyRowViewModel>
            {
                new TPR2010EnergyRowViewModel("6MV"),
                new TPR2010EnergyRowViewModel("10MV"),
                new TPR2010EnergyRowViewModel("6FFF"),
            };
        }
    }
}
