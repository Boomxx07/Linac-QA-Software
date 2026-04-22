// Purpose: ViewModel for a single beam energy in the EDW Output Factor QA test.
//
// Owns:
//   • The table of EDWOutputFactorRowViewModels (9 rows: Open, and 15/30/45/60 In/Out)
//
// When the user enters data, this class uses the Open field average to recalculate 
// the output factors for the wedged fields in response to RowUpdated events.

using Linac_QA_Software.Helpers;
using Linac_QA_Software.Models;
using System;
using System.Collections.ObjectModel;

namespace Linac_QA_Software.ViewModels
{
    public class EDWOutputFactorEnergyConfigViewModel : ValidatedObservableObject
    {
        // -------------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------------

        public string EnergyName { get; }
        public string? MeasurementInstruction { get; }

        // -------------------------------------------------------------------------
        // Table rows 
        // -------------------------------------------------------------------------

        public ObservableCollection<EDWOutputFactorRowViewModel> Rows { get; }

        // Results for the merged columns
        public EDWResult Wedge15Result { get; } = new EDWResult();
        public EDWResult Wedge30Result { get; } = new EDWResult();
        public EDWResult Wedge45Result { get; } = new EDWResult();
        public EDWResult Wedge60Result { get; } = new EDWResult();

        // -------------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------------

        public EDWOutputFactorEnergyConfigViewModel(string energyName)
        {
            EnergyName = energyName;
            Rows = new ObservableCollection<EDWOutputFactorRowViewModel>();
            for (int i = 0; i < 9; i++)
            {
                var row = new EDWOutputFactorRowViewModel();
                row.RowUpdated += (s, e) => CalculateFactors();
                Rows.Add(row);
            }
        }

        // -------------------------------------------------------------------------
        // Private methods
        // -------------------------------------------------------------------------

        private void CalculateFactors()
        {
            float? openAvg = Rows[0].Average;

            // Wedge calculations: (AvgIN + AvgOUT)/2 / OpenAvg
            UpdateWedgeResult(Wedge15Result, openAvg, (Rows[1].Average + Rows[2].Average) / 2f, "15");
            UpdateWedgeResult(Wedge30Result, openAvg, (Rows[3].Average + Rows[4].Average) / 2f, "30");
            UpdateWedgeResult(Wedge45Result, openAvg, (Rows[5].Average + Rows[6].Average) / 2f, "45");
            UpdateWedgeResult(Wedge60Result, openAvg, (Rows[7].Average + Rows[8].Average) / 2f, "60");
        }

        private void UpdateWedgeResult(EDWResult result, float? openAvg, float? wedgeAvg, string label)
        {
            if (openAvg == null || wedgeAvg == null || openAvg == 0)
            {
                result.OutputFactor = null; result.PercentDiff = null; result.StatusText = "";
                return;
            }

            result.OutputFactor = wedgeAvg / openAvg;

            var config = ConfigLoader.Load("config.json");
            var test = config.Tests.FirstOrDefault(t => t.Name == "EDWOutputFactor");
            float baseline = 1.0f;
            if (test?.FieldSizeThresholds != null && test.FieldSizeThresholds.TryGetValue(label, out var threshold))
                baseline = threshold.Baseline;

            result.PercentDiff = ((result.OutputFactor - baseline) / baseline) * 100f;
            result.StatusText = StatusEvaluator.EvaluateRelative(result.OutputFactor.Value, baseline, test?.Fail ?? 3.0f, test?.Caution ?? 2.0f).ToString();
        }
    }
}