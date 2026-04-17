// Purpose: ViewModel for a single row in the linearity data table.
//
// Each row represents one MU setting (e.g. 5 MU, 10 MU, 200 MU).
// The user enters up to three electrometer readings; this class computes
// the average, leakage-corrected value, reading-per-MU, and pass/fail status.

using System;
using System.Linq;
using Linac_QA_Software.Helpers;
using Linac_QA_Software.Models;

namespace Linac_QA_Software.ViewModels
{
    public class LinearityRowViewModel : ValidatedObservableObject
    {
        // -------------------------------------------------------------------------
        // Event — raised whenever any calculated value changes so that the parent
        // EnergyConfigViewModel knows to refresh the chart and regression.
        // -------------------------------------------------------------------------
        public event EventHandler RowUpdated;

        // -------------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------------

        /// <summary>The energyName corresponding to this section.</summary>
        public string EnergyName { get; }
        /// <summary>The monitor-unit setting this row represents (e.g. 5, 10, 200).</summary>
        public float MU { get; }
        
        // -------------------------------------------------------------------------
        // User-entered readings (stored as strings to preserve input format, including trailing zeros)
        // -------------------------------------------------------------------------

        private string _reading1 = "", _reading2 = "", _reading3 = "";

        public string Reading1
        {
            get => _reading1;
            set
            {
                if (SetProperty(ref _reading1, value))
                {
                    ValidateNumeric(value, nameof(Reading1));
                    Recalculate();
                }
            }
        }

        public string Reading2
        {
            get => _reading2;
            set
            {
                if (SetProperty(ref _reading2, value))
                {
                    ValidateNumeric(value, nameof(Reading2));
                    Recalculate();
                }
            }
        }

        public string Reading3
        {
            get => _reading3;
            set
            {
                if (SetProperty(ref _reading3, value))
                {
                    ValidateNumeric(value, nameof(Reading3));
                    Recalculate();
                }
            }
        }

        // -------------------------------------------------------------------------
        // Calculated outputs (all read-only from the UI's perspective)
        // -------------------------------------------------------------------------

        private float? _average, _leakageCorrected, _readingPerMu, _percentDiff;

        /// <summary>Mean of whichever readings have been entered.</summary>
        public float? Average { get => _average; private set => SetProperty(ref _average, value); }

        /// <summary>Average reading minus the expected leakage during the delivery time.</summary>
        public float? LeakageCorrected { get => _leakageCorrected; private set => SetProperty(ref _leakageCorrected, value); }

        /// <summary>LeakageCorrected divided by MU — the normalised output used for linearity comparison.</summary>
        public float? ReadingPerMU { get => _readingPerMu; private set => SetProperty(ref _readingPerMu, value); }

        /// <summary>
        /// Percent difference of ReadingPerMU relative to the 200 MU reference row.
        /// Positive = higher output than reference; negative = lower.
        /// </summary>
        public float? PercentDiff
        {
            get => _percentDiff;
            private set
            {
                if (SetProperty(ref _percentDiff, value))
                    OnPropertyChanged(nameof(StatusText)); // StatusText depends on PercentDiff
            }
        }

        /// <summary>
        /// Pass/fail string derived from PercentDiff.
        /// Empty when no data is available; "OK" within ±2 %; "FAIL" otherwise.
        /// </summary>
        public string StatusText => PercentDiff.HasValue
            ? (Math.Abs(PercentDiff.Value) <= PhysicsCalculator.PercentDiffTolerance ? "OK" : "FAIL")
            : "";

        // -------------------------------------------------------------------------
        // Internal state
        // -------------------------------------------------------------------------

        /// <summary>
        /// The leakage rate in nC/s, supplied by the parent EnergyConfigViewModel.
        /// Stored separately so that recalculation works even when the user hasn't
        /// entered all readings yet.
        /// </summary>
        private float _leakageRate = 0f;

        // -------------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------------

        public LinearityRowViewModel(int mu, string energyName)
        {
            MU = mu;
            EnergyName = energyName;
        }

        // -------------------------------------------------------------------------
        // Methods called by the parent ViewModel
        // -------------------------------------------------------------------------

        /// <summary>
        /// Called by EnergyConfigViewModel whenever the leakage rate changes.
        /// Triggers a recalculation so leakage correction stays current.
        /// </summary>
        public void UpdateLeakageRate(float leakageRateNcPerSec)
        {
            _leakageRate = leakageRateNcPerSec;
            Recalculate();
        }

        /// <summary>
        /// Called by EnergyConfigViewModel to set this row's percent difference
        /// relative to the 200 MU reference row.
        /// </summary>
        public void UpdatePercentDiff(float referenceReadingPerMu)
        {
            if (ReadingPerMU.HasValue && Math.Abs(referenceReadingPerMu) > 1e-10)
                PercentDiff = ((ReadingPerMU.Value - referenceReadingPerMu) / referenceReadingPerMu) * 100f;
            else
                PercentDiff = null;
        }

        // -------------------------------------------------------------------------
        // Private recalculation logic
        // -------------------------------------------------------------------------

        /// <summary>
        /// Safely converts a string to float?, returning null if empty or invalid.
        /// </summary>
        private static float? ParseNumeric(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return float.TryParse(value, out float result) ? result : null;
        }

        private void Recalculate()
        {
            // Parse string readings to nullable floats
            var reading1 = ParseNumeric(Reading1);
            var reading2 = ParseNumeric(Reading2);
            var reading3 = ParseNumeric(Reading3);

            var presentReadings = new[] { reading1, reading2, reading3 }
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .ToList();

            if (presentReadings.Count > 0)
            {
                Average = presentReadings.Average();

                //Debugging code to check what doserate MU is being collected
                //System.Diagnostics.Debug.WriteLine(EnergyName);
                //System.Diagnostics.Debug.WriteLine(PhysicsCalculator.DoseRateMuPerSec(EnergyName));

                // Estimated beam-on time in seconds based on the assumed dose rate.
                float deliveryTimeSec = (MU / PhysicsCalculator.DoseRateMuPerSec(EnergyName));
                LeakageCorrected = Average + (_leakageRate * deliveryTimeSec);
                ReadingPerMU = LeakageCorrected / MU;
            }
            else
            {
                // No readings yet — reset all derived values.
                Average = LeakageCorrected = ReadingPerMU = null;
            }

            RowUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}