using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Linac_QA_Software.ViewModels
{
    // ----------------------------------------------------------------------
    // 1. BASE CLASS: Handles INotifyPropertyChanged cleanly
    // ----------------------------------------------------------------------
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    // ----------------------------------------------------------------------
    // 2. MAIN VIEWMODEL: Binds to the root UserControl
    // ----------------------------------------------------------------------
    public class LinearityViewModel : ObservableObject
    {
        public ObservableCollection<EnergyConfiguration> EnergyConfigs { get; }

        public LinearityViewModel()
        {
            EnergyConfigs = new ObservableCollection<EnergyConfiguration>();

            // Initialize with the standard Photon MUs from your screenshot.
            // When you are ready for electrons, simply clear this and load the electron configurations.
            int[] standardPhotonMUs = { 5, 10, 50, 100, 200, 400, 900 };

            EnergyConfigs.Add(new EnergyConfiguration("6MV", standardPhotonMUs));
            EnergyConfigs.Add(new EnergyConfiguration("10MV", standardPhotonMUs));
            EnergyConfigs.Add(new EnergyConfiguration("6FFF", standardPhotonMUs));
        }
    }

    // ----------------------------------------------------------------------
    // 3. ENERGY CONFIGURATION: Manages one specific energy block (e.g., 6MV)
    // ----------------------------------------------------------------------
    public class EnergyConfiguration : ObservableObject
    {
        public string EnergyName { get; }
        public ObservableCollection<LinearityRowData> LinearityDataRows { get; }

        // --- Leakage Inputs ---
        private double? _leakageTime1;
        public double? LeakageTime1
        {
            get => _leakageTime1;
            set { if (SetProperty(ref _leakageTime1, value)) CalculateLeakageRate(); }
        }

        private double? _leakageReading1;
        public double? LeakageReading1
        {
            get => _leakageReading1;
            set { if (SetProperty(ref _leakageReading1, value)) CalculateLeakageRate(); }
        }

        private double? _leakageTime2;
        public double? LeakageTime2
        {
            get => _leakageTime2;
            set { if (SetProperty(ref _leakageTime2, value)) CalculateLeakageRate(); }
        }

        private double? _leakageReading2;
        public double? LeakageReading2
        {
            get => _leakageReading2;
            set { if (SetProperty(ref _leakageReading2, value)) CalculateLeakageRate(); }
        }

        // --- Leakage Calculated Result ---
        private double? _calculatedLeakageRate;
        public double? CalculatedLeakageRate
        {
            get => _calculatedLeakageRate;
            private set
            {
                if (SetProperty(ref _calculatedLeakageRate, value))
                {
                    // When the overall leakage rate changes, push the update down to all rows
                    foreach (var row in LinearityDataRows)
                    {
                        row.UpdateLeakageRate(value ?? 0);
                    }
                    RecalculateRelativeDifferences();
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
                // Subscribe to row changes so we can trigger the relative 200MU check
                row.RowAveragesCalculated += OnRowAveragesCalculated;
                LinearityDataRows.Add(row);
            }
        }

        private void CalculateLeakageRate()
        {
            double totalRate = 0;
            int validReadings = 0;

            if (LeakageTime1 > 0 && LeakageReading1.HasValue)
            {
                totalRate += LeakageReading1.Value / LeakageTime1.Value;
                validReadings++;
            }

            if (LeakageTime2 > 0 && LeakageReading2.HasValue)
            {
                totalRate += LeakageReading2.Value / LeakageTime2.Value;
                validReadings++;
            }

            if (validReadings > 0)
            {
                CalculatedLeakageRate = totalRate / validReadings;
            }
            else
            {
                CalculatedLeakageRate = null;
            }
        }

        private void OnRowAveragesCalculated(object sender, EventArgs e)
        {
            // Whenever any row finishes recalculating its base average/leakage, 
            // we must evaluate the % difference relative to the 200 MU baseline.
            RecalculateRelativeDifferences();
        }

        private void RecalculateRelativeDifferences()
        {
            // Find the 200 MU reference row
            var refRow = LinearityDataRows.FirstOrDefault(r => r.MU == 200);

            if (refRow == null || !refRow.ReadingPerMU.HasValue || refRow.ReadingPerMU.Value == 0)
            {
                // If 200 MU isn't filled out yet, clear the % diffs
                foreach (var row in LinearityDataRows) row.ClearRelativeDifference();
                return;
            }

            double referenceValue = refRow.ReadingPerMU.Value;

            foreach (var row in LinearityDataRows)
            {
                row.CalculateRelativeDifference(referenceValue);
            }
        }
    }

    // ----------------------------------------------------------------------
    // 4. ROW DATA: Manages a single MU line (e.g., MU = 50)
    // ----------------------------------------------------------------------
    public class LinearityRowData : ObservableObject
    {
        public int MU { get; }

        // Event fired to let the parent EnergyConfiguration know math needs to happen
        public event EventHandler RowAveragesCalculated;

        private double _currentLeakageRate = 0;

        // --- Inputs ---
        private double? _reading1;
        public double? Reading1
        {
            get => _reading1;
            set { if (SetProperty(ref _reading1, value)) RecalculateInternal(); }
        }

        private double? _reading2;
        public double? Reading2
        {
            get => _reading2;
            set { if (SetProperty(ref _reading2, value)) RecalculateInternal(); }
        }

        private double? _reading3;
        public double? Reading3
        {
            get => _reading3;
            set { if (SetProperty(ref _reading3, value)) RecalculateInternal(); }
        }

        // --- Calculated Properties ---
        private double? _average;
        public double? Average
        {
            get => _average;
            private set => SetProperty(ref _average, value);
        }

        private double? _leakageCorrected;
        public double? LeakageCorrected
        {
            get => _leakageCorrected;
            private set => SetProperty(ref _leakageCorrected, value);
        }

        private double? _readingPerMU;
        public double? ReadingPerMU
        {
            get => _readingPerMU;
            private set => SetProperty(ref _readingPerMU, value);
        }

        private double? _percentDiff;
        public double? PercentDiff
        {
            get => _percentDiff;
            private set { if (SetProperty(ref _percentDiff, value)) OnPropertyChanged(nameof(StatusText)); }
        }

        // Fails if > 2.0% difference (Adjust tolerance as needed)
        public string StatusText
        {
            get
            {
                if (!PercentDiff.HasValue) return string.Empty;
                return Math.Abs(PercentDiff.Value) <= 2.0 ? "OK" : "FAIL";
            }
        }

        public LinearityRowData(int mu)
        {
            MU = mu;
        }

        public void UpdateLeakageRate(double leakageRate)
        {
            _currentLeakageRate = leakageRate;
            RecalculateInternal();
        }

        private void RecalculateInternal()
        {
            // Calculate Average of non-null inputs
            double sum = 0;
            int count = 0;

            if (Reading1.HasValue) { sum += Reading1.Value; count++; }
            if (Reading2.HasValue) { sum += Reading2.Value; count++; }
            if (Reading3.HasValue) { sum += Reading3.Value; count++; }

            if (count > 0)
            {
                Average = sum / count;

                // NOTE: Proper leakage correction requires the time of irradiation. 
                // Assuming standard 600 MU/min dose rate for the calculation here.
                // Replace "600" with your actual nominal dose rate if needed.
                double timeInSeconds = (MU / 600.0) * 60.0;
                LeakageCorrected = Average - (_currentLeakageRate * timeInSeconds);

                ReadingPerMU = LeakageCorrected / MU;
            }
            else
            {
                Average = null;
                LeakageCorrected = null;
                ReadingPerMU = null;
            }

            // Notify parent to run the 200MU relative calculations
            RowAveragesCalculated?.Invoke(this, EventArgs.Empty);
        }

        public void CalculateRelativeDifference(double referenceValuePerMU)
        {
            if (ReadingPerMU.HasValue && referenceValuePerMU != 0)
            {
                PercentDiff = ((ReadingPerMU.Value - referenceValuePerMU) / referenceValuePerMU) * 100.0;
            }
            else
            {
                PercentDiff = null;
            }
        }

        public void ClearRelativeDifference()
        {
            PercentDiff = null;
        }
    }
}