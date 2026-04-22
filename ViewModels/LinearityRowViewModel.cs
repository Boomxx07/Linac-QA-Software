using System;
using System.Linq;
using Linac_QA_Software.Helpers;
using Linac_QA_Software.Models;

namespace Linac_QA_Software.ViewModels
{
    public class LinearityRowViewModel : BaseReadingRowViewModel
    {
        private float _leakageRate;
        private float? _referenceReadingPerMu;

        public event EventHandler RowUpdated;

        public string EnergyName { get; }
        public float MU { get; }
        public bool IsReference => Math.Abs(MU - 200) < 0.1;

        private float? _percentDiff;
        public float? PercentDiff
        {
            get => _percentDiff;
            private set => SetProperty(ref _percentDiff, value);
        }

        // Renamed from StatusText to StatusDisplay to fix the "Type vs Variable" error
        private string _statusDisplay;
        public string StatusDisplay
        {
            get => _statusDisplay;
            private set => SetProperty(ref _statusDisplay, value);
        }

        public LinearityRowViewModel(string energyName, float mu)
        {
            EnergyName = energyName;
            MU = mu;
        }

        public void UpdateLeakage(float newRate)
        {
            _leakageRate = newRate;
            Recalculate();
        }

        public void UpdatePercentDiff(float referenceReadingPerMu)
        {
            _referenceReadingPerMu = referenceReadingPerMu;
            if (ReadingPerMU.HasValue && !IsReference)
            {
                PercentDiff = (float)((ReadingPerMU.Value - _referenceReadingPerMu.Value) / _referenceReadingPerMu.Value * 100.0);
            }
            else
            {
                PercentDiff = IsReference ? 0 : null;
            }
            EvaluateStatus();
        }

        /// <summary>
        /// Fixed: Implements the abstract member from BaseReadingRowViewModel
        /// and uses StatusDisplay to avoid naming conflicts.
        /// </summary>
        public override void EvaluateStatus()
        {
            if (IsReference)
            {
                StatusDisplay = "Ref";
                return;
            }

            if (!PercentDiff.HasValue)
            {
                StatusDisplay = "";
                return;
            }

            // Using clinical tolerances (1% Caution, 2% Fail)
            float absDiff = Math.Abs(PercentDiff.Value);
            if (absDiff > 2.0f)
            {
                StatusDisplay = "FAIL";
            }
            else if (absDiff > 1.0f)
            {
                StatusDisplay = "CAUTION";
            }
            else
            {
                StatusDisplay = "OK";
            }
        }

        private void Recalculate()
        {
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

                // Estimated beam-on time: MU / (MU/sec)
                float doseRate = PhysicsCalculator.DoseRateMuPerSec(EnergyName);
                float deliveryTimeSec = MU / doseRate;

                LeakageCorrected = Average + (_leakageRate * deliveryTimeSec);
                ReadingPerMU = LeakageCorrected / MU;

                // If we already have a reference baseline, update the diff and status
                if (_referenceReadingPerMu.HasValue)
                {
                    UpdatePercentDiff(_referenceReadingPerMu.Value);
                }
            }
            else
            {
                Average = LeakageCorrected = ReadingPerMU = PercentDiff = null;
                StatusDisplay = "";
            }

            RowUpdated?.Invoke(this, EventArgs.Empty);
        }

        private float? ParseNumeric(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return float.TryParse(value, out float result) ? result : null;
        }
    }
}