// Purpose: ViewModel for a single energy's TPR2010 measurements.
//
// Owns:
//   • Two TPR2010RowViewModels (one for depth 20cm, one for depth 10cm)
//   • Calculated TPR2010 value (average at 20cm / average at 10cm)
//   • Pass/fail status based on energy-specific thresholds
//
// When the user enters data, this class recalculates the TPR2010 value.

using System;
using System.Collections.Generic;
using System.Linq;
using Linac_QA_Software.Helpers;
using Linac_QA_Software.Models;

namespace Linac_QA_Software.ViewModels
{
    public class TPR2010EnergyRowViewModel : ValidatedObservableObject
    {
        // -------------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------------

        /// <summary>Display name for this beam energy (e.g., "6MV", "10MV", "6FFF").</summary>
        public string EnergyName { get; }

        // -------------------------------------------------------------------------
        // Measurement rows — one for depth 20cm, one for depth 10cm
        // -------------------------------------------------------------------------

        /// <summary>Row for SSD=80, Depth=20cm measurement.</summary>
        public TPR2010RowViewModel Row80_20 { get; }

        /// <summary>Row for SSD=90, Depth=10cm measurement.</summary>
        public TPR2010RowViewModel Row90_10 { get; }

        // -------------------------------------------------------------------------
        // Configuration (loaded from config.json)
        // -------------------------------------------------------------------------
        private static Config _config;
        private static TestConfig _tpr2010Config;

        static TPR2010EnergyRowViewModel()
        {
            _config = ConfigLoader.Load("config.json");
            _tpr2010Config = _config.Tests.FirstOrDefault(t => t.Name == "TPR2010");
        }

        // -------------------------------------------------------------------------
        // Calculated outputs
        // -------------------------------------------------------------------------

        private float? _tpr2010Value;

        /// <summary>TPR2010 value: Average(20cm) / Average(10cm).</summary>
        public float? TPR2010Value
        {
            get => _tpr2010Value;
            private set
            {
                if (SetProperty(ref _tpr2010Value, value))
                    OnPropertyChanged(nameof(PercentDiff));
            }
        }

        private float? _percentDiff;

        /// <summary>Percent difference from baseline TPR2010 value.</summary>
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
        /// Pass/fail string derived from TPR2010 deviation from baseline.
        /// Uses energy-specific thresholds from config.json.
        /// </summary>
        public string StatusText
        {
            get
            {
                if (!PercentDiff.HasValue || _tpr2010Config == null)
                    return "";

                // Try to get energy-specific thresholds
                float baseline = _tpr2010Config.Baseline;
                float caution = _tpr2010Config.Caution;
                float fail = _tpr2010Config.Fail;

                // If field size thresholds exist (using energy name as key)
                if (_tpr2010Config.FieldSizeThresholds != null &&
                    _tpr2010Config.FieldSizeThresholds.TryGetValue(EnergyName, out var energyThreshold))
                {
                    baseline = energyThreshold.Baseline;
                    caution = energyThreshold.Caution;
                    fail = energyThreshold.Fail;
                }

                return StatusEvaluator.EvaluateRelative(PercentDiff.Value, baseline, failDiff: fail, cautionaryDiff: caution).ToString();
            }
        }

        // -------------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------------

        public TPR2010EnergyRowViewModel(string energyName)
        {
            EnergyName = energyName;

            // Create measurement rows for this energy
            Row80_20 = new TPR2010RowViewModel(energyName, 80, 20);
            Row90_10 = new TPR2010RowViewModel(energyName, 90, 10);

            // Subscribe to row updates
            Row80_20.RowUpdated += (_, _) => RecalculateTPR2010();
            Row90_10.RowUpdated += (_, _) => RecalculateTPR2010();
        }

        // -------------------------------------------------------------------------
        // Private methods
        // -------------------------------------------------------------------------

        /// <summary>
        /// Recalculates the TPR2010 value and percent difference whenever
        /// measurements change.
        /// </summary>
        private void RecalculateTPR2010()
        {
            // Calculate TPR2010 as ratio of averages
            TPR2010Value = TPR2010Calculator.CalculateTPR2010(Row80_20.Average, Row90_10.Average);

            // Calculate percent difference from baseline
            if (TPR2010Value.HasValue && _tpr2010Config != null)
            {
                float baseline = _tpr2010Config.Baseline;

                // Use energy-specific baseline if available
                if (_tpr2010Config.FieldSizeThresholds != null &&
                    _tpr2010Config.FieldSizeThresholds.TryGetValue(EnergyName, out var energyThreshold))
                {
                    baseline = energyThreshold.Baseline;
                }

                if (Math.Abs(baseline) > 1e-10)
                {
                    PercentDiff = ((TPR2010Value.Value - baseline) / baseline) * 100f;
                }
                else
                {
                    PercentDiff = null;
                }
            }
            else
            {
                PercentDiff = null;
            }
        }
    }
}
