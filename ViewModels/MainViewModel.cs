// Purpose: The root ViewModel bound to MainWindow.
//
// Holds general session metadata (which linac, which date, which physicists)
// and acts as a container for the sub-ViewModels of each QA test tab.
// Commands for Save and Submit are defined here because they concern the
// whole session, not any individual test.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Linac_QA_Software.Helpers;

namespace Linac_QA_Software.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        // -------------------------------------------------------------------------
        // Sub-ViewModels (expand this as more test tabs are added)
        // -------------------------------------------------------------------------

        /// <summary>ViewModel for the Linearity QA tab.</summary>
        public LinearityViewModel LinearityVM { get; }

        // -------------------------------------------------------------------------
        // Session metadata
        // -------------------------------------------------------------------------

        /// <summary>Machine names available in the linac dropdown.</summary>
        public ObservableCollection<string> LinacOptions { get; } = new()
        {
            "Acacia", "Banksia", "Tuart", "Jarrah", "Marri"
        };

        private string _linac = "";
        /// <summary>The linac selected for this session.</summary>
        public string Linac
        {
            get => _linac;
            set => SetProperty(ref _linac, value);
        }

        private DateTime _date = DateTime.Today;
        /// <summary>Date of the QA session (defaults to today).</summary>
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        /// <summary>All physicists who may be listed as attending.</summary>
        public ObservableCollection<string> PhysicistList { get; } = new(
            new[]
            {
                "Alison Scott",       "Andrew Hirst",     "Brani Rusanov",       "Broderick McCallum-Hee",
                "Gabor Neveri",       "Gavin Pikes",      "Godfrey Mukwada",     "John Geraghty",
                "Luke Slama",         "Mahsheed Sabet",   "Malgorzata Skorska",  "Mounir Ibrahim",
                "Nathaniel Barry",    "Sivakumar Somangili", "Talat Mahmood",    "Tom Milan",
                "Zaid Alkhatib"
            }.OrderBy(x => x)
        );

        /// <summary>
        /// Physicists ticked as present for this session.
        /// Populated by the ListBoxHelper attached property in XAML.
        /// </summary>
        public ObservableCollection<string> SelectedPhysicists { get; } = new();

        // -------------------------------------------------------------------------
        // Commands
        // -------------------------------------------------------------------------

        public ICommand SaveCommand { get; }
        public ICommand SubmitCommand { get; }

        // -------------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------------

        public MainViewModel()
        {
            LinearityVM = new LinearityViewModel();
            SaveCommand = new RelayCommand(Save);
            SubmitCommand = new RelayCommand(Submit);
        }

        // -------------------------------------------------------------------------
        // Command handlers
        // -------------------------------------------------------------------------

        private void Save()
        {
            string physicists = SelectedPhysicists.Any()
                ? string.Join(", ", SelectedPhysicists)
                : "None selected";

            // TODO: Replace with real persistence (database, XML, PDF export, etc.)
            MessageBox.Show(
                $"Saved (stub)\nLinac: {Linac}\nDate: {Date:d}\nPhysicists: {physicists}",
                "Save");
        }

        private void Submit()
        {
            // TODO: Replace with real submission/export logic
            MessageBox.Show("Submitted (stub)", "Submit");
        }
    }
}