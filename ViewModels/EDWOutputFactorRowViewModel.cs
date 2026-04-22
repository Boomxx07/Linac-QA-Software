// Purpose: ViewModel for a single row in the EDW output factor data table.
//
// Calculates the average of up to three electrometer readings, the EDW Output Factor 
// (relative to the open field), the % difference from baseline, and pass/fail status.

using System;
using System.Linq;
using Linac_QA_Software.Helpers;
using Linac_QA_Software.Models;

namespace Linac_QA_Software.ViewModels
{
    public class EDWOutputFactorRowViewModel : ValidatedObservableObject
    {
        public event EventHandler RowUpdated;

        private string _reading1 = "", _reading2 = "", _reading3 = "";
        private float? _average;

        public string Reading1 { get => _reading1; set { if (SetProperty(ref _reading1, value)) Recalculate(); } }
        public string Reading2 { get => _reading2; set { if (SetProperty(ref _reading2, value)) Recalculate(); } }
        public string Reading3 { get => _reading3; set { if (SetProperty(ref _reading3, value)) Recalculate(); } }

        public float? Average { get => _average; private set => SetProperty(ref _average, value); }

        private void Recalculate()
        {
            var readings = new[] { _reading1, _reading2, _reading3 }
                .Select(s => float.TryParse(s, out float r) ? r : (float?)null)
                .Where(r => r.HasValue).ToList();

            Average = readings.Count > 0 ? readings.Average() : null;
            RowUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}