// Purpose: ViewModel for a single beam energy (e.g. "6MV", "10MV", "6FFF").
//
// Owns:
//   • The table of LinearityRowViewModels (one per MU setting)
//   • Leakage measurement inputs and the derived leakage rate
//   • The LiveCharts2 series data and regression result
//
// When the user enters data, this class recalculates the regression and
// updates the chart in response to the RowUpdated events from each row.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Linac_QA_Software.Helpers;
using Linac_QA_Software.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Linac_QA_Software.ViewModels
{
    public class EnergyConfigViewModel : ObservableObject
    {
        // -------------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------------

        /// <summary>Display name for this beam energy, e.g. "6MV".</summary>
        public string EnergyName { get; }

        // -------------------------------------------------------------------------
        // Table rows — one per MU setting
        // -------------------------------------------------------------------------

        public ObservableCollection<LinearityRowViewModel> Rows { get; }

        // -------------------------------------------------------------------------
        // Leakage measurement inputs
        // -------------------------------------------------------------------------

        private double? _leakageTime1, _leakageReading1, _leakageTime2, _leakageReading2;

        public double? LeakageTime1
        {
            get => _leakageTime1;
            set { if (SetProperty(ref _leakageTime1, value)) RefreshLeakageRate(); }
        }
        public double? LeakageReading1
        {
            get => _leakageReading1;
            set { if (SetProperty(ref _leakageReading1, value)) RefreshLeakageRate(); }
        }
        public double? LeakageTime2
        {
            get => _leakageTime2;
            set { if (SetProperty(ref _leakageTime2, value)) RefreshLeakageRate(); }
        }
        public double? LeakageReading2
        {
            get => _leakageReading2;
            set { if (SetProperty(ref _leakageReading2, value)) RefreshLeakageRate(); }
        }

        private double? _calculatedLeakageRate;
        /// <summary>
        /// Average leakage rate in nC/s derived from the two background measurements.
        /// Null until at least one valid measurement is entered.
        /// </summary>
        public double? CalculatedLeakageRate
        {
            get => _calculatedLeakageRate;
            private set
            {
                if (!SetProperty(ref _calculatedLeakageRate, value)) return;

                // Push the new rate into every row so they can recalculate.
                double rate = value ?? 0.0;
                foreach (var row in Rows)
                    row.UpdateLeakageRate(rate);
            }
        }

        // -------------------------------------------------------------------------
        // Chart properties (bound to a LiveCharts2 CartesianChart in XAML)
        // -------------------------------------------------------------------------

        /// <summary>
        /// The two chart series: scattered data points and a linear trendline.
        /// Backed by the private ObservableCollections below so LiveCharts2
        /// can animate additions/removals automatically.
        /// </summary>
        public ObservableCollection<ISeries> Series { get; }

        public Axis[] XAxes { get; }
        public Axis[] YAxes { get; }

        // Internal point lists referenced by the chart series.
        private readonly ObservableCollection<ObservablePoint> _dataPoints = new();
        private readonly ObservableCollection<ObservablePoint> _trendPoints = new();

        // -------------------------------------------------------------------------
        // Regression result properties (displayed below the chart)
        // -------------------------------------------------------------------------

        private double _rSquared;
        public double RSquared
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

            // Build one row per MU value and subscribe to its update event.
            Rows = new ObservableCollection<LinearityRowViewModel>();
            foreach (int mu in muValues)
            {
                var row = new LinearityRowViewModel(mu);
                row.RowUpdated += (_, _) => RefreshChartAndRegression();
                Rows.Add(row);
            }

            // Set up the chart series once, referencing the shared point collections.
            // LiveCharts2 watches these ObservableCollections for changes.
            Series = new ObservableCollection<ISeries>
            {
                new ScatterSeries<ObservablePoint>
                {
                    Name       = "Measurements",
                    Values     = _dataPoints,
                    GeometrySize = 12,
                    Fill       = new SolidColorPaint(SKColors.DodgerBlue),
                    ZIndex     = 2
                },
                new LineSeries<ObservablePoint>
                {
                    Name            = "Linear Fit",
                    Values          = _trendPoints,
                    GeometrySize    = 0,
                    Fill            = null,
                    Stroke          = new SolidColorPaint(SKColors.Crimson, 3),
                    LineSmoothness  = 0,
                    ZIndex          = 1
                }
            };

            XAxes = new[] { new Axis { Name = "Monitor Units (MU)" } };
            YAxes = new[] { new Axis { Name = "Corrected Reading (nC)" } };
        }

        // -------------------------------------------------------------------------
        // Private methods
        // -------------------------------------------------------------------------

        /// <summary>
        /// Recalculates the leakage rate from the two background measurements
        /// and propagates the result to all rows.
        /// </summary>
        private void RefreshLeakageRate()
        {
            CalculatedLeakageRate = PhysicsCalculator.CalculateLeakageRate(
                LeakageReading1, LeakageTime1,
                LeakageReading2, LeakageTime2);
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

                // Rebuild scatter-plot points.
                _dataPoints.Clear();
                foreach (var row in validRows)
                    _dataPoints.Add(new ObservablePoint(row.MU, row.LeakageCorrected!.Value));

                // Need at least two points to fit a line.
                if (validRows.Count < 2)
                {
                    _trendPoints.Clear();
                    RSquared = 0;
                    RegressionEquation = "Need 2+ data points";
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

                // Draw the trendline from the first to the last valid MU value.
                double minX = validRows.First().MU;
                double maxX = validRows.Last().MU;
                _trendPoints.Clear();
                _trendPoints.Add(new ObservablePoint(minX, result.Slope * minX + result.Intercept));
                _trendPoints.Add(new ObservablePoint(maxX, result.Slope * maxX + result.Intercept));

                // Update percent differences relative to the 200 MU reference row.
                var refRow = Rows.FirstOrDefault(r => r.MU == 200);
                if (refRow?.ReadingPerMU != null)
                {
                    foreach (var row in Rows)
                        row.UpdatePercentDiff(refRow.ReadingPerMU.Value);
                }
            });
        }
    }
}