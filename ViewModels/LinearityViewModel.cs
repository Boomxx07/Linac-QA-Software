using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Linac_QA_Software.ViewModels
{
    public class LinearityViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // FIXED: Removed the recursive call to OnPropertyChanged(nameof(LeakageRate))
        // that was causing an infinite loop.
        private void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private double _startTime;
        public double StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;

                    // EMERGENCY DEBUG: If this box doesn't pop up when you type, 
                    // the UI is not successfully sending data to this property.
                    System.Windows.MessageBox.Show($"C# received: {value}");

                    OnPropertyChanged();
                    // Notify the UI that LeakageRate needs to be recalculated
                    OnPropertyChanged(nameof(LeakageRate));
                }
            }
        }

        private double _endTime;
        public double EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LeakageRate));
                }
            }
        }

        private double _startReading;
        public double StartReading
        {
            get => _startReading;
            set
            {
                if (_startReading != value)
                {
                    _startReading = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LeakageRate));
                }
            }
        }

        private double _endReading;
        public double EndReading
        {
            get => _endReading;
            set
            {
                if (_endReading != value)
                {
                    _endReading = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LeakageRate));
                }
            }
        }

        public double LeakageRate
        {
            get
            {
                double dt = EndTime - StartTime;
                // Safety check to avoid division by zero or negative time
                if (dt <= 0) return 0;
                return (EndReading - StartReading) / dt;
            }
        }
    }
}