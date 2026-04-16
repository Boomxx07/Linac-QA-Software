using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Linac_QA_Software.ViewModels
{
    // ----------------------------------------------------------------------
    // 1. BASE CLASS
    // ----------------------------------------------------------------------
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    // ----------------------------------------------------------------------
    // 2. MAIN VIEWMODEL
    // ----------------------------------------------------------------------
    public class LinearityViewModel : ObservableObject
    {
        public ObservableCollection<EnergyConfiguration> EnergyConfigs { get; }

        public LinearityViewModel()
        {
            EnergyConfigs = new ObservableCollection<EnergyConfiguration>();
            // Standard monitor unit values for photon linearity
            int[] photonMUs = { 5, 10, 50, 100, 200, 400, 900 };

            EnergyConfigs.Add(new EnergyConfiguration("6MV", photonMUs));
            EnergyConfigs.Add(new EnergyConfiguration("10MV", photonMUs));
            EnergyConfigs.Add(new EnergyConfiguration("6FFF", photonMUs));
        }
    }

    // ----------------------------------------------------------------------
    // 3. ENERGY CONFIGURATION (The DataContext for each Item in ItemsControl)
    // ----------------------------------------------------------------------
    public class EnergyConfiguration : ObservableObject
    {
        public string EnergyName { get; }
        public ObservableCollection<LinearityRowData> LinearityDataRows { get; }

        // LiveCharts2 Properties - Must be properties for XAML Binding
        private ObservableCollection<ISeries> _series;
        public ObservableCollection<ISeries> Series { get => _series; set => SetProperty(ref _series, value); }

        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        private double _rSquared;
        public double RSquared { get => _rSquared; set => SetProperty(ref _rSquared, value); }

        private string _regressionEquation = "y = mx + c";
        public string RegressionEquation { get => _regressionEquation; set => SetProperty(ref _regressionEquation, value); }

        // Core data collections initialized once to maintain object references
        private readonly ObservableCollection<ObservablePoint> _dataPoints = new();
        private readonly ObservableCollection<ObservablePoint> _trendlinePoints = new();

        // Leakage logic properties
        private double? _lTime1, _lReading1, _lTime2, _lReading2;
        public double? LeakageTime1 { get => _lTime1; set { if (SetProperty(ref _lTime1, value)) CalculateLeakageRate(); } }
        public double? LeakageReading1 { get => _lReading1; set { if (SetProperty(ref _lReading1, value)) CalculateLeakageRate(); } }
        public double? LeakageTime2 { get => _lTime2; set { if (SetProperty(ref _lTime2, value)) CalculateLeakageRate(); } }
        public double? LeakageReading2 { get => _lReading2; set { if (SetProperty(ref _lReading2, value)) CalculateLeakageRate(); } }

        private double? _calcLeakageRate;
        public double? CalculatedLeakageRate
        {
            get => _calcLeakageRate;
            private set
            {
                if (SetProperty(ref _calcLeakageRate, value))
                {
                    foreach (var row in LinearityDataRows) row.UpdateLeakageRate(value ?? 0);
                }
            }
        }

        public EnergyConfiguration(string name, int[] muValues)
        {
            EnergyName = name;
            LinearityDataRows = new ObservableCollection<LinearityRowData>();

            foreach (var mu in muValues)
            {
                var row = new LinearityRowData(mu);
                row.RowUpdated += (s, e) => UpdateChartAndCalculations();
                LinearityDataRows.Add(row);
            }

            SetupChart();
        }

        private void SetupChart()
        {
            // Binding the Series once to our internal ObservableCollections
            Series = new ObservableCollection<ISeries>
            {
                new ScatterSeries<ObservablePoint>
                {
                    Name = "Measurements",
                    Values = _dataPoints,
                    GeometrySize = 12,
                    Fill = new SolidColorPaint(SKColors.DodgerBlue),
                    ZIndex = 2
                },
                new LineSeries<ObservablePoint>
                {
                    Name = "Linear Fit",
                    Values = _trendlinePoints,
                    GeometrySize = 0,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.Crimson, 3),
                    LineSmoothness = 0,
                    ZIndex = 1
                }
            };

            XAxes = new[] { new Axis { Name = "Monitor Units (MU)" } };
            YAxes = new[] { new Axis { Name = "Corr. Avg (nC)" } };
        }

        private void CalculateLeakageRate()
        {
            double total = 0; int count = 0;
            if (LeakageTime1 > 0 && LeakageReading1.HasValue) { total += LeakageReading1.Value / LeakageTime1.Value; count++; }
            if (LeakageTime2 > 0 && LeakageReading2.HasValue) { total += LeakageReading2.Value / LeakageTime2.Value; count++; }
            CalculatedLeakageRate = count > 0 ? total / count : null;
        }

        private void UpdateChartAndCalculations()
        {
            // Use Dispatcher to ensure collection updates happen on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                var validRows = LinearityDataRows
                    .Where(r => r.LeakageCorrected.HasValue)
                    .OrderBy(r => r.MU)
                    .ToList();

                _dataPoints.Clear();
                foreach (var r in validRows)
                {
                    _dataPoints.Add(new ObservablePoint(r.MU, r.LeakageCorrected.Value));
                }

                if (validRows.Count < 2)
                {
                    _trendlinePoints.Clear();
                    RSquared = 0;
                    RegressionEquation = "Need 2+ points";
                    return;
                }

                // Linear Regression Math
                int n = validRows.Count;
                double sumX = validRows.Sum(r => (double)r.MU);
                double sumY = validRows.Sum(r => r.LeakageCorrected.Value);
                double sumXY = validRows.Sum(r => r.MU * r.LeakageCorrected.Value);
                double sumXX = validRows.Sum(r => (double)r.MU * r.MU);
                double sumYY = validRows.Sum(r => r.LeakageCorrected.Value * r.LeakageCorrected.Value);

                double denominator = (n * sumXX - sumX * sumX);
                if (Math.Abs(denominator) < 1e-10) return;

                double slope = (n * sumXY - sumX * sumY) / denominator;
                double intercept = (sumY - slope * sumX) / n;

                double rNum = (n * sumXY - sumX * sumY);
                double rDen = Math.Sqrt((n * sumXX - sumX * sumX) * (n * sumYY - sumY * sumY));
                RSquared = rDen != 0 ? Math.Pow(rNum / rDen, 2) : 0;
                RegressionEquation = $"y = {slope:F5}x + {intercept:F5}";

                _trendlinePoints.Clear();
                double minX = (double)validRows.First().MU;
                double maxX = (double)validRows.Last().MU;
                _trendlinePoints.Add(new ObservablePoint(minX, slope * minX + intercept));
                _trendlinePoints.Add(new ObservablePoint(maxX, slope * maxX + intercept));

                // Update percent differences relative to 200 MU
                var refRow = LinearityDataRows.FirstOrDefault(r => r.MU == 200);
                if (refRow?.ReadingPerMU != null)
                {
                    foreach (var row in LinearityDataRows)
                        row.CalculateRelativeDifference(refRow.ReadingPerMU.Value);
                }
            });
        }
    }

    // ----------------------------------------------------------------------
    // 4. ROW DATA
    // ----------------------------------------------------------------------
    public class LinearityRowData : ObservableObject
    {
        public int MU { get; }
        public event EventHandler RowUpdated;

        private double _leakageRate = 0;
        private double? _r1, _r2, _r3;

        public double? Reading1 { get => _r1; set { if (SetProperty(ref _r1, value)) Recalculate(); } }
        public double? Reading2 { get => _r2; set { if (SetProperty(ref _r2, value)) Recalculate(); } }
        public double? Reading3 { get => _r3; set { if (SetProperty(ref _r3, value)) Recalculate(); } }

        private double? _avg, _corr, _rdgMu, _pDiff;
        public double? Average { get => _avg; private set => SetProperty(ref _avg, value); }
        public double? LeakageCorrected { get => _corr; private set => SetProperty(ref _corr, value); }
        public double? ReadingPerMU { get => _rdgMu; private set => SetProperty(ref _rdgMu, value); }
        public double? PercentDiff { get => _pDiff; set { if (SetProperty(ref _pDiff, value)) OnPropertyChanged(nameof(StatusText)); } }

        public string StatusText => !PercentDiff.HasValue ? "" : Math.Abs(PercentDiff.Value) <= 2.0 ? "OK" : "FAIL";

        public LinearityRowData(int mu) => MU = mu;

        public void UpdateLeakageRate(double rate) { _leakageRate = rate; Recalculate(); }

        private void Recalculate()
        {
            var readings = new[] { Reading1, Reading2, Reading3 }.Where(r => r.HasValue).ToList();
            if (readings.Any())
            {
                Average = readings.Average();
                // Physics assumption: 10 MU/sec for typical linac dose rates
                double time = MU / 10.0;
                LeakageCorrected = Average - (_leakageRate * time);
                ReadingPerMU = LeakageCorrected / MU;
            }
            else { Average = LeakageCorrected = ReadingPerMU = null; }

            RowUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void CalculateRelativeDifference(double refVal)
        {
            if (ReadingPerMU.HasValue && Math.Abs(refVal) > 1e-10)
                PercentDiff = ((ReadingPerMU.Value - refVal) / refVal) * 100.0;
            else
                PercentDiff = null;
        }
    }
}