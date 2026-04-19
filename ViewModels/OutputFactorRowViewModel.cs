// Purpose: ViewModel for a single row in the output factor data table.
//
// Each row represents one field size measurement (e.g. 3x3, 7x7, 10x10, etc.)
// The user enters up to three electrometer readings; this class computes
// the average, output factor, and pass/fail status.

using System;
using System.Linq;
using Linac_QA_Software.Helpers;
using Linac_QA_Software.Models;

namespace Linac_QA_Software.ViewModels
{
    public class OutputFactorRowViewModel : ValidatedObservableObject
    {
        // -------------------------------------------------------------------------
        // Event — raised whenever any calculated value changes so that the parent
        // OutputFactorEnergyConfigViewModel knows to refresh calculations and charts.
        // -------------------------------------------------------------------------
        public event EventHandler RowUpdated;

        // -------------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------------

        /// <summary>The energyName corresponding to this section.</summary>
        public string EnergyName { get; }
        /// <summary>The X field size (cm).</summary>
        public float X { get; }
        /// <summary>The Y field size (cm).</summary>
        public float Y { get; }
        /// <summary>Whether this is an MLC-defined field.</summary>
        public bool IsMLC { get; }

        /// <summary>Display label for this row (e.g. "10×10", "3×5 (MLC)").</summary>
        public string FieldLabel => IsMLC ? $"{X:F0}×{Y:F0} (MLC)" : $"{X:F0}×{Y:F0}";

        /// <summary>Whether this is a symmetric field (X == Y).</summary>
        public bool IsSymmetric => Math.Abs(X - Y) < 0.01f;

        /// <summary>Square field size for symmetric fields, or average for asymmetric.</summary>
        public float SquareFieldSize => (X + Y) / 2f;

        // -------------------------------------------------------------------------
        // Configuration (loaded from config.json)
        // -------------------------------------------------------------------------
        private static Config _config;
        private static TestConfig _outputFactorConfig;

        static OutputFactorRowViewModel()
        {
            _config = ConfigLoader.Load("config.json");
            _outputFactorConfig = _config.Tests.FirstOrDefault(t => t.Name == "OutputFactor");
        }

        // -------------------------------------------------------------------------
        // User-entered readings (stored as strings to preserve input format)
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

        private float? _average, _outputFactor;

        /// <summary>Mean of whichever readings have been entered.</summary>
        public float? Average { get => _average; private set => SetProperty(ref _average, value); }

        /// <summary>Output factor as ratio to reference (10x10) average.</summary>
        public float? OutputFactor
        {
            get => _outputFactor;
            private set
            {
                if (SetProperty(ref _outputFactor, value))
                    OnPropertyChanged(nameof(StatusText)); // StatusText depends on OutputFactor
            }
        }

        /// <summary>
        /// Pass/fail string derived from OutputFactor deviation from baseline.
        /// Uses field-size-specific thresholds from config.json if available,
        /// otherwise falls back to default test thresholds.
        /// </summary>
        public string StatusText
        {
            get
            {
                if (!OutputFactor.HasValue || _outputFactorConfig == null)
                    return "";

                // Try to get field-size-specific thresholds using the FieldLabel as key
                // This matches the config.json format (e.g. "3x3", "3x3 (MLC)", "3x5", etc.)
                float baseline = _outputFactorConfig.Baseline;
                float caution = _outputFactorConfig.Caution;
                float fail = _outputFactorConfig.Fail;

                if (_outputFactorConfig.FieldSizeThresholds != null &&
                    _outputFactorConfig.FieldSizeThresholds.TryGetValue(FieldLabel, out var fieldThreshold))
                {
                    baseline = fieldThreshold.Baseline;
                    caution = fieldThreshold.Caution;
                    fail = fieldThreshold.Fail;
                }

                return StatusEvaluator.EvaluateRelative(OutputFactor.Value, baseline, failDiff: fail, cautionaryDiff: caution).ToString();
            }
        }

        // -------------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------------

        public OutputFactorRowViewModel(float x, float y, bool isMLC, string energyName)
        {
            X = x;
            Y = y;
            IsMLC = isMLC;
            EnergyName = energyName;
        }

        // -------------------------------------------------------------------------
        // Methods called by the parent ViewModel
        // -------------------------------------------------------------------------

        /// <summary>
        /// Called by OutputFactorEnergyConfigViewModel to set this row's output factor
        /// based on the reference (10x10) average.
        /// </summary>
        public void UpdateOutputFactor(float? referenceAverage)
        {
            OutputFactor = OutputFactorCalculator.CalculateOutputFactor(Average, referenceAverage);
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
            }
            else
            {
                // No readings yet — reset all derived values.
                Average = null;
                OutputFactor = null;
            }

            RowUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
