// Purpose: ViewModel for a single beam energy (e.g. "6MV", "10MV", "6FFF").
//
// Owns:
//   • The table of LinearityRowViewModels (one per MU setting)
//   • Leakage measurement inputs and the derived leakage rate
//   • The OxyPlot information
//
// When the user enters data, this class recalculates the regression and
// updates the chart in response to the RowUpdated events from each row.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Linac_QA_Software.Helpers;
using Linac_QA_Software.Models;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace Linac_QA_Software.ViewModels
{
    public class EnergyConfigViewModel : ValidatedObservableObject
    {
        // -------------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------------

        /// <summary>Display name for this beam energy, e.g. "6MV".</summary>
        public string EnergyName { get; }
        public string MeasurementInstruction { get; }

        // -------------------------------------------------------------------------
        // Table rows — one per MU setting
        // -------------------------------------------------------------------------

        public ObservableCollection<LinearityRowViewModel> Rows { get; }

        // -------------------------------------------------------------------------
        // Leakage measurement inputs (stored as strings to preserve user input format)
        // -------------------------------------------------------------------------

        private string _leakageTime1 = "", _leakageReading1 = "", _leakageTime2 = "", _leakageReading2 = "";

        public string LeakageTime1
        {
            get => _leakageTime1;
            set
            {
                if (SetProperty(ref _leakageTime1, value))
                {
                    ValidateNumeric(value, nameof(LeakageTime1));
                    RefreshLeakageRate();
                }
            }
        }

        public string LeakageReading1
        {
            get => _leakageReading1;
            set
            {
                if (SetProperty(ref _leakageReading1, value))
                {
                    ValidateNumeric(value, nameof(LeakageReading1));
                    RefreshLeakageRate();
                }
            }
        }

        public string LeakageTime2
        {
            get => _leakageTime2;
            set
            {
                if (SetProperty(ref _leakageTime2, value))
                {
                    ValidateNumeric(value, nameof(LeakageTime2));
                    RefreshLeakageRate();
                }
            }
        }

        public string LeakageReading2
        {
            get => _leakageReading2;
            set
            {
                if (SetProperty(ref _leakageReading2, value))
                {
                    ValidateNumeric(value, nameof(LeakageReading2));
                    RefreshLeakageRate();
                }
            }
        }

        private float? _calculatedLeakageRate;
        /// <summary>
        /// Average leakage rate in nC/s derived from the two background measurements.
        /// Null until at least one valid measurement is entered.
        /// </summary>
        public float? CalculatedLeakageRate
        {
            get => _calculatedLeakageRate;
            private set
            {
                if (!SetProperty(ref _calculatedLeakageRate, value)) return;

                // Push the new rate into every row so they can recalculate.
                float rate = value ?? 0.0f;
                foreach (var row in Rows)
                    row.UpdateLeakageRate(rate);
            }
        }

        // -------------------------------------------------------------------------
        // Chart (OxyPlot) properties
        // -------------------------------------------------------------------------

        private PlotModel _plotModel;
        public PlotModel PlotModel
        {
            get => _plotModel;
            private set => SetProperty(ref _plotModel, value);
        }

        private ScatterSeries _scatterSeries;
        private LineSeries _trendLine;

        // -------------------------------------------------------------------------
        // Regression result properties (displayed below the chart)
        // -------------------------------------------------------------------------

        private float _rSquared;
        public float RSquared
        {
            get => _rSquared;
            private set => SetProperty(ref _rSquared, value);
        }

        private string _regressionEquation = "Enter data to see fit";
        public string RegressionEquation
        {
            get => _regressionEquation;
            private set => SetProperty(ref _regressionEquation, value);
        }

        // -------------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------------

        public EnergyConfigViewModel(string energyName, int[] muValues)
        {
            EnergyName = energyName;

            if (energyName == "6FFF")
            {
                MeasurementInstruction = "Field Size: 10 cm x 10 cm, SSD: 10 cm, Depth: 10 cm, Dose Rate: 1400 MU/min";
            }
            else MeasurementInstruction = "Field Size: 10 cm x 10 cm, SSD: 10 cm, Depth: 10 cm, Dose Rate: 600 MU/min";

            // Build one row per MU value and subscribe to its update event.
            Rows = new ObservableCollection<LinearityRowViewModel>();
            foreach (int mu in muValues)
            {
                var row = new LinearityRowViewModel(mu, energyName);
                row.RowUpdated += (_, _) => RefreshChartAndRegression();
                Rows.Add(row);
            }

            InitialisePlot();
        }

        // -------------------------------------------------------------------------
        // Private methods
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

        /// <summary>
        /// Recalculates the leakage rate from the two background measurements
        /// and propagates the result to all rows.
        /// </summary>
        private void RefreshLeakageRate()
        {
            var time1 = ParseNumeric(LeakageTime1);
            var reading1 = ParseNumeric(LeakageReading1);
            var time2 = ParseNumeric(LeakageTime2);
            var reading2 = ParseNumeric(LeakageReading2);

            CalculatedLeakageRate = PhysicsCalculator.CalculateLeakageRate(
                reading1, time1,
                reading2, time2);
        }

        /// <summary>
        /// Sets up the OxyPlot
        /// </summary>
        private void InitialisePlot()
        {
            _scatterSeries = new ScatterSeries
            {
                Title = "Measurements",
                MarkerType = MarkerType.Circle,
                MarkerSize = 4
            };

            _trendLine = new LineSeries
            {
                Title = "Linear Fit",
                StrokeThickness = 2
            };

            PlotModel = new PlotModel
            {
                Title = $"{EnergyName} Linearity"
            };

            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Monitor Units (MU)"
            });

            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Corrected Reading (nC)"
            });

            PlotModel.Series.Add(_scatterSeries);
            PlotModel.Series.Add(_trendLine);
        }

        /// <summary>
        /// Rebuilds the chart data points and recalculates the linear regression
        /// whenever any row's readings change.  Runs on the UI thread because
        /// ObservableCollection mutations must occur there for WPF to notice them.
        /// </summary>
        private void RefreshChartAndRegression()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Only include rows that have a leakage-corrected value.
                var validRows = Rows
                    .Where(r => r.LeakageCorrected.HasValue)
                    .OrderBy(r => r.MU)
                    .ToList();

                // Clear current points and line then readd
                _scatterSeries.Points.Clear();
                _trendLine.Points.Clear();

                foreach (var row in validRows)
                {
                    _scatterSeries.Points.Add(
                        new ScatterPoint(row.MU, row.LeakageCorrected!.Value));
                }

                // Need at least two points to fit a line.
                if (validRows.Count < 2)
                {
                    RSquared = 0;
                    RegressionEquation = "Need 2+ data points";
                    PlotModel.InvalidatePlot(true);
                    return;
                }

                // Delegate the math to the Model layer.
                var regressionInput = validRows
                    .Select(r => new LinearityPoint { MU = r.MU, CorrectedReading = r.LeakageCorrected!.Value })
                    .ToList();

                var result = PhysicsCalculator.CalculateRegression(regressionInput);
                if (result == null) return;

                RSquared = result.RSquared;
                RegressionEquation = result.Equation;

                // Plotting
                float minX = validRows.First().MU;
                float maxX = validRows.Last().MU;

                _trendLine.Points.Add(new DataPoint(minX, result.Slope * minX + result.Intercept));
                _trendLine.Points.Add(new DataPoint(maxX, result.Slope * maxX + result.Intercept));

                var refRow = Rows.FirstOrDefault(r => r.MU == 200);
                if (refRow?.ReadingPerMU != null)
                {
                    foreach (var row in Rows)
                        row.UpdatePercentDiff(refRow.ReadingPerMU.Value);
                }

                PlotModel.InvalidatePlot(true);
            });
        }
    }
}