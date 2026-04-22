// Purpose: ViewModel for a single beam energy in the Output Factor QA test.
//
// Owns:
//   • The table of OutputFactorRowViewModels (one per field size)
//   • The OxyPlot information for two charts:
//     1. Output factors for symmetric fields (jaw and MLC defined)
//     2. Measured vs baseline for asymmetric fields
//
// When the user enters data, this class recalculates the output factors
// and updates the charts in response to the RowUpdated events from each row.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Linac_QA_Software.Helpers;
using Linac_QA_Software.Models;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Wpf;

namespace Linac_QA_Software.ViewModels
{
    public class OutputFactorEnergyConfigViewModel : ValidatedObservableObject
    {
        // -------------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------------

        /// <summary>Display name for this beam energy, e.g. "6MV".</summary>
        public string EnergyName { get; }
        public string MeasurementInstruction { get; }

        // -------------------------------------------------------------------------
        // Table rows — one per field size
        // -------------------------------------------------------------------------

        public ObservableCollection<OutputFactorRowViewModel> Rows { get; }

        // -------------------------------------------------------------------------
        // Chart (OxyPlot) properties
        // -------------------------------------------------------------------------

        private PlotModel _symmetricFieldsPlotModel;
        public PlotModel SymmetricFieldsPlotModel
        {
            get => _symmetricFieldsPlotModel;
            private set => SetProperty(ref _symmetricFieldsPlotModel, value);
        }

        private PlotModel _asymmetricFieldsPlotModel;
        public PlotModel AsymmetricFieldsPlotModel
        {
            get => _asymmetricFieldsPlotModel;
            private set => SetProperty(ref _asymmetricFieldsPlotModel, value);
        }

        private LineSeries _jawDefinedSeries;
        private LineSeries _mlcDefinedSeries;
        private BarSeries _asymmetricSeriesMeasured;
        private BarSeries _asymmetricSeriesBaseline;

        // -------------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------------

        public OutputFactorEnergyConfigViewModel(string energyName)
        {
            EnergyName = energyName;
            MeasurementInstruction = energyName == "6FFF"
                ? "Field Sizes: As per protocol, SSD: 100 cm, Depth: 10 cm, Dose Rate: 1400 MU/min"
                : "Field Sizes: As per protocol, SSD: 100 cm, Depth: 10 cm, Dose Rate: 600 MU/min";

            // Define the field sizes to measure (X, Y, IsMLC)
            var fieldSizes = new (float, float, bool)[]
            {
                (10, 10, false),
                (3, 3, false),
                (7, 7, false),
                (20, 20, false),
                (40, 40, false),
                (3, 5, false),
                (5, 7, false),
                (40, 7, false),
                (3, 3, true),
                (2, 2, true),
                (1, 1, true),
                (10, 10, false),  // Second 10x10 for reference averaging
            };

            // Build one row per field size and subscribe to its update event.
            Rows = new ObservableCollection<OutputFactorRowViewModel>();
            foreach (var (x, y, isMLC) in fieldSizes)
            {
                var row = new OutputFactorRowViewModel(x, y, isMLC, energyName);
                row.RowUpdated += (_, _) => RefreshChartsAndOutputFactors();
                Rows.Add(row);
            }

            InitialisePlots(energyName);
        }

        // -------------------------------------------------------------------------
        // Private methods
        // -------------------------------------------------------------------------

        /// <summary>
        /// Sets up the two OxyPlot charts
        /// </summary>
        private void InitialisePlots(string energyName)
        {
            // ===== Chart 1: Symmetric Fields =====
            _jawDefinedSeries = new LineSeries
            {
                Title = "Jaw Defined",
                MarkerType = MarkerType.Circle,
                MarkerSize = 5,
                StrokeThickness = 2
            };

            _mlcDefinedSeries = new LineSeries
            {
                Title = "MLC Defined",
                MarkerType = MarkerType.Square,
                MarkerSize = 5,
                StrokeThickness = 2
            };

            SymmetricFieldsPlotModel = new PlotModel
            {
                Title = $"{energyName} OFs - Symmetric"
            };

            SymmetricFieldsPlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Square Field Size (cm)"
            });

            SymmetricFieldsPlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Output Factor"
            });

            SymmetricFieldsPlotModel.Series.Add(_jawDefinedSeries);
            SymmetricFieldsPlotModel.Series.Add(_mlcDefinedSeries);

            // ===== Chart 2: Asymmetric Fields =====
            // BarSeries requires CategoryAxis on Y-axis (left)
            _asymmetricSeriesMeasured = new BarSeries
            {
                Title = "Measured",
                FillColor = OxyColor.FromRgb(100, 150, 255)
            };

            _asymmetricSeriesBaseline = new BarSeries
            {
                Title = "Baseline",
                FillColor = OxyColor.FromRgb(150, 255, 150)
            };

            AsymmetricFieldsPlotModel = new PlotModel
            {
                Title = $"{energyName} OFs - Asymmetric"
            };

            // For BarSeries, CategoryAxis must be on the Y-axis (left/vertical)
            AsymmetricFieldsPlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Output Factor"
            });

            AsymmetricFieldsPlotModel.Axes.Add(new CategoryAxis
            {
                Position = AxisPosition.Left,
                Title = "Field Size"
            });

            AsymmetricFieldsPlotModel.Series.Add(_asymmetricSeriesMeasured);
            AsymmetricFieldsPlotModel.Series.Add(_asymmetricSeriesBaseline);
        }

        /// <summary>
        /// Refreshes all chart data and recalculates output factors whenever
        /// any row's readings change.
        /// </summary>
        private void RefreshChartsAndOutputFactors()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Find the reference 10x10 readings (there are two of them)
                var reference10x10Rows = Rows
                    .Where(r => Math.Abs(r.X - 10) < 0.01f && Math.Abs(r.Y - 10) < 0.01f && !r.IsMLC)
                    .ToList();

                // Use the first 10x10 reading if available; use average of both if both are available
                float? referenceAverage = null;
                if (reference10x10Rows.Count >= 1 && reference10x10Rows[0].Average.HasValue)
                {
                    if (reference10x10Rows.Count == 2 && reference10x10Rows[1].Average.HasValue)
                    {
                        // Both readings available — use their average
                        referenceAverage = (reference10x10Rows[0].Average.Value + reference10x10Rows[1].Average.Value) / 2f;
                    }
                    else
                    {
                        // Only first reading available — use it
                        referenceAverage = reference10x10Rows[0].Average.Value;
                    }
                }

                // Update all rows with the reference average
                foreach (var row in Rows)
                {
                    row.UpdateOutputFactor(referenceAverage);
                }

                // Refresh symmetric fields chart
                _jawDefinedSeries.Points.Clear();
                _mlcDefinedSeries.Points.Clear();

                var symmetricRows = Rows
                    .Where(r => r.IsSymmetric && r.OutputFactor.HasValue)
                    .OrderBy(r => r.SquareFieldSize)
                    .ToList();

                foreach (var row in symmetricRows)
                {
                    var point = new DataPoint(row.SquareFieldSize, row.OutputFactor!.Value);
                    if (row.IsMLC)
                        _mlcDefinedSeries.Points.Add(point);
                    else
                        _jawDefinedSeries.Points.Add(point);
                }

                SymmetricFieldsPlotModel.InvalidatePlot(true);

                // Refresh asymmetric fields chart
                _asymmetricSeriesMeasured.Items.Clear();
                _asymmetricSeriesBaseline.Items.Clear();

                var asymmetricRows = Rows
                    .Where(r => !r.IsSymmetric && r.OutputFactor.HasValue)
                    .OrderBy(r => r.FieldLabel)
                    .ToList();

                // CategoryAxis is now at position 1 (Left/Y-axis) for BarSeries
                var categoryAxis = AsymmetricFieldsPlotModel.Axes[1] as CategoryAxis;
                if (categoryAxis != null)
                {
                    categoryAxis.Labels.Clear();
                    for (int i = 0; i < asymmetricRows.Count; i++)
                    {
                        categoryAxis.Labels.Add(asymmetricRows[i].FieldLabel);
                        _asymmetricSeriesMeasured.Items.Add(new BarItem(asymmetricRows[i].OutputFactor!.Value));

                        // Use the config baseline value (nominally 1.0)
                        var config = ConfigLoader.Load("config.json");
                        var outputFactorConfig = config.Tests.FirstOrDefault(t => t.Name == "OutputFactor");
                        float baseline = outputFactorConfig?.Baseline ?? 1.0f;
                        _asymmetricSeriesBaseline.Items.Add(new BarItem(baseline));
                    }
                }

                AsymmetricFieldsPlotModel.InvalidatePlot(true);
            });
        }
    }
}
