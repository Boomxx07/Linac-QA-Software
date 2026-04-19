// Purpose: ViewModel for a single TPR2010 measurement row (one depth at one SSD).
//
// Each row represents a measurement at a specific SSD and depth combination
// (e.g., 6MV at SSD=80cm, Depth=20cm).
// The user enters up to three electrometer readings; this class computes the average.

using System;
using System.Linq;
using Linac_QA_Software.Helpers;

namespace Linac_QA_Software.ViewModels
{
    public class TPR2010RowViewModel : ValidatedObservableObject
    {
        // -------------------------------------------------------------------------
        // Event — raised whenever any calculated value changes
        // -------------------------------------------------------------------------
        public event EventHandler RowUpdated;

        // -------------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------------

        /// <summary>The energy name (e.g., "6MV", "10MV", "6FFF").</summary>
        public string EnergyName { get; }

        /// <summary>The SSD in cm (80 or 90).</summary>
        public float SSD { get; }

        /// <summary>The measurement depth in cm (10 or 20).</summary>
        public float Depth { get; }

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
        // Calculated outputs (read-only from the UI's perspective)
        // -------------------------------------------------------------------------

        private float? _average;

        /// <summary>Mean of whichever readings have been entered.</summary>
        public float? Average
        {
            get => _average;
            private set => SetProperty(ref _average, value);
        }

        // -------------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------------

        public TPR2010RowViewModel(string energyName, float ssd, float depth)
        {
            EnergyName = energyName;
            SSD = ssd;
            Depth = depth;
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
            }

            RowUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
