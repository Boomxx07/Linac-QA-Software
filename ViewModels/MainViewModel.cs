using Linac_QA_Software.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Linac_QA_Software.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public LinearityViewModel LinearityVM { get; set; }

        public MainViewModel()
        {
            LinearityVM = new LinearityViewModel();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // -------------------------
        // Variables for the general QA information
        // -------------------------

        public ObservableCollection<string> LinacOptions { get; } = new ObservableCollection<string>
        {
            "Acacia", "Banksia", "Tuart", "Jarrah", "Marri"
        };

        private string _linac = "";
        public string Linac
        {
            get => _linac;
            set
            {
                if (_linac != value)
                {
                    _linac = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _date = DateTime.Today;
        public DateTime Date
        {
            get => _date;
            set
            {
                if (_date != value)
                {
                    _date = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> PhysicistList { get; set; } = new ObservableCollection<string>(
            new[]
            {
                "Alison Scott", "Andrew Hirst", "Brani Rusanov", "Broderick McCallum-Hee",
                "Gabor Neveri", "Gavin Pikes", "Godfrey Mukwada", "John Geraghty",
                "Luke Slama", "Mahsheed Sabet", "Malgorzata Skorska", "Mounir Ibrahim",
                "Nathaniel Barry", "Sivakumar Somangili", "Talat Mahmood", "Tom Milan", "Zaid Alkhatib"
            }.OrderBy(x => x)
        );

        public ObservableCollection<string> SelectedPhysicists { get; set; } = new ObservableCollection<string>();

        // -------------------------
        // Commands
        // -------------------------
        public ICommand SaveCommand => new RelayCommand(Save);
        public ICommand SubmitCommand => new RelayCommand(Submit);

        private void Save()
        {
            string physicists = SelectedPhysicists.Any() ? string.Join(", ", SelectedPhysicists) : "None selected";
            MessageBox.Show($"Saved (stub)\nLinac: {Linac}\nDate: {Date:d}\nPhysicists: {physicists}");
        }

        private void Submit()
        {
            MessageBox.Show("Submitted (stub)");
        }
    }
}