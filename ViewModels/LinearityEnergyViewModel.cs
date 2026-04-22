using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Linac_QA_Software.Helpers;
using Linac_QA_Software.Models;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace Linac_QA_Software.ViewModels
{
    public class LinearityEnergyViewModel : ValidatedObservableObject
    {
        private readonly LinearityData _sessionData;
        private readonly ThresholdSet _thresholds;

        public string EnergyName => _sessionData.EnergyName;
        public string MeasurementInstruction { get; }
        public ObservableCollection<LinearityRowViewModel> Rows { get; }

        private string _leakageTime1 = "", _leakageReading1 = "", _leakageTime2 = "", _leakageReading2 = "";
        public string LeakageTime1 { get => _leakageTime1; set { if (SetProperty(ref _leakageTime1, value)) { ValidateNumeric(value); RefreshLeakageRate(); } } }
        public string LeakageReading1 { get => _leakageReading1; set { if (SetProperty(ref _leakageReading1, value)) { ValidateNumeric(value); RefreshLeakageRate(); } } }
        public string LeakageTime2 { get => _leakageTime2; set { if (SetProperty(ref _leakageTime2, value)) { ValidateNumeric(value); RefreshLeakageRate(); } } }
        public string LeakageReading2 { get => _leakageReading2; set { if (SetProperty(ref _leakageReading2, value)) { ValidateNumeric(value); RefreshLeakageRate(); } } }

        private double? _calculatedLeakageRate;
        private ScatterSeries _scatterSeries;
        private LineSeries _trendLine;

        public double? CalculatedLeakageRate
        {
            get => _calculatedLeakageRate;
            private set
            {
                if (SetProperty(ref _calculatedLeakageRate, value))
                {
                    double rate = value ?? 0.0;
                    foreach (var row in Rows) row.UpdateLeakageRate(rate);
                }
            }
        }

        public PlotModel PlotModel { get; private set; }
        public double RSquared => _sessionData.RSquared;
        public string RegressionEquation => _sessionData.Points.Count(p => p.CorrectedReading > 0) >= 2
            ? $"y = {_sessionData.Slope:F5}x + {_sessionData.Intercept:F5}" : "Need 2+ points";

        public LinearityEnergyViewModel(string energyName, double[] muValues, Config config)
        {
            _sessionData = new LinearityData { EnergyName = energyName };

            // Extract Linearity thresholds from the Config object
            var testConfig = config.Tests?.FirstOrDefault(t => t.Name == "Linearity");
            _thresholds = testConfig?.GlobalThreshold ?? new ThresholdSet { Caution = 1.0f, Fail = 2.0f };

            MeasurementInstruction = energyName.Contains("FFF")
                ? "Field Size: 10x10 cm, SSD: 100 cm, Depth: 10 cm, Dose Rate: 1400 MU/min"
                : "Field Size: 10x10 cm, SSD: 100 cm, Depth: 10 cm, Dose Rate: 600 MU/min";

            Rows = new ObservableCollection<LinearityRowViewModel>();
            foreach (double mu in muValues)
            {
                var point = new LinearityPoint { MU = mu };
                _sessionData.Points.Add(point);
                var rowVM = new LinearityRowViewModel(point, _sessionData);
                rowVM.PropertyChanged += Row_PropertyChanged;
                Rows.Add(rowVM);
            }
            InitialisePlot();
        }

        private void Row_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LinearityRowViewModel.PercentDiff))
            {
                // Re-evaluate status for ALL rows because the 200MU reference might have changed
                foreach (var row in Rows)
                {
                    row.EvaluateStatus(_thresholds);
                }
                RefreshChartAndRegression();
            }
        }

        private void RefreshLeakageRate()
        {
            CalculatedLeakageRate = PhysicsCalculator.CalculateLeakage(
                ParseNumeric(LeakageReading1), ParseNumeric(LeakageTime1),
                ParseNumeric(LeakageReading2), ParseNumeric(LeakageTime2));
        }

        private static double? ParseNumeric(string v) => double.TryParse(v, out double r) ? r : null;

        // -------------------------------------------------------------------------
        // OxyPlot Handling
        // -------------------------------------------------------------------------
        private void InitialisePlot()
        {
            _scatterSeries = new ScatterSeries { Title = "Measurements", MarkerType = MarkerType.Circle, MarkerSize = 4 };
            _trendLine = new LineSeries { Title = "Linear Fit", StrokeThickness = 2 };

            PlotModel = new PlotModel { Title = $"{EnergyName} Linearity" };
            PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Monitor Units (MU)" });
            PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Corrected Reading (nC)" });

            PlotModel.Series.Add(_scatterSeries);
            PlotModel.Series.Add(_trendLine);
        }

        private void RefreshChartAndRegression()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _scatterSeries.Points.Clear();
                _trendLine.Points.Clear();

                var validPoints = _sessionData.Points
                    .Where(p => p.CorrectedReading > 0)
                    .OrderBy(p => p.MU)
                    .ToList();

                foreach (var p in validPoints)
                {
                    _scatterSeries.Points.Add(new ScatterPoint(p.MU, p.CorrectedReading));
                }

                if (validPoints.Count >= 2)
                {
                    double minX = validPoints.First().MU;
                    double maxX = validPoints.Last().MU;

                    _trendLine.Points.Add(new DataPoint(minX, (_sessionData.Slope * minX) + _sessionData.Intercept));
                    _trendLine.Points.Add(new DataPoint(maxX, (_sessionData.Slope * maxX) + _sessionData.Intercept));
                }

                OnPropertyChanged(nameof(RSquared));
                OnPropertyChanged(nameof(RegressionEquation));

                PlotModel.InvalidatePlot(true);
            });
        }
    }
}