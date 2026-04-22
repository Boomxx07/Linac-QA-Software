using Linac_QA_Software.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Linac_QA_Software.ViewModels
{
    /// <summary>
    /// Base class for any ViewModel representing a row of electrometer data.
    /// Handles the standard three-reading input, averaging, and validation.
    /// </summary>
    public abstract class BaseReadingRowViewModel : ValidatedObservableObject
    {
        private string _reading1 = string.Empty;
        private string _reading2 = string.Empty;
        private string _reading3 = string.Empty;
        private double _average;
        private StatusText _status = StatusText.OK;

        public string Reading1
        {
            get => _reading1;
            set
            {
                if (SetProperty(ref _reading1, value))
                {
                    ValidateNumeric(value);
                    CalculateAverage();
                }
            }
        }

        public string Reading2
        {
            get => _reading2;
            set
            {
                if (SetProperty(ref _reading2, value))
                {
                    ValidateNumeric(value);
                    CalculateAverage();
                }
            }
        }

        public string Reading3
        {
            get => _reading3;
            set
            {
                if (SetProperty(ref _reading3, value))
                {
                    ValidateNumeric(value);
                    CalculateAverage();
                }
            }
        }

        public double Average
        {
            get => _average;
            protected set => SetProperty(ref _average, value);
        }

        public StatusText Status
        {
            get => _status;
            protected set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// Logic for calculating the mean of the three readings.
        /// Ignores non-numeric inputs.
        /// </summary>
        protected virtual void CalculateAverage()
        {
            double sum = 0;
            int count = 0;

            if (double.TryParse(Reading1, out double r1)) { sum += r1; count++; }
            if (double.TryParse(Reading2, out double r2)) { sum += r2; count++; }
            if (double.TryParse(Reading3, out double r3)) { sum += r3; count++; }

            Average = count > 0 ? sum / count : 0;

            // Trigger the status evaluation after the average updates
            EvaluateStatus();
        }

        /// <summary>
        /// Must be implemented by derived classes (Linearity, Output, etc.)
        /// to define how the Average compares to a specific Baseline/Tolerance.
        /// </summary>
        public abstract void EvaluateStatus();
    }
}