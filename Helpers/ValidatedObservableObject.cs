// Purpose: Base class for ViewModels that need validation support.
//
// Extends ObservableObject with INotifyDataErrorInfo to track and report
// validation errors per property. WPF automatically highlights TextBoxes
// with red borders when errors are present.
//
// Usage:
//   public class MyViewModel : ValidatedObservableObject
//   {
//       private string _myNumericField;
//       public string MyNumericField
//       {
//           get => _myNumericField;
//           set
//           {
//               if (SetProperty(ref _myNumericField, value))
//               {
//                   ValidateNumeric(nameof(MyNumericField), value);
//               }
//           }
//       }
//   }

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Linac_QA_Software.Helpers
{
    public abstract class ValidatedObservableObject : ObservableObject, INotifyDataErrorInfo
    {
        /// <summary>
        /// Dictionary mapping property names to lists of validation errors for that property.
        /// </summary>
        private readonly Dictionary<string, List<string>> _errors = new();

        /// <summary>
        /// Raised whenever the error state changes for any property.
        /// WPF uses this to trigger validation visual feedback (red borders, etc).
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// Returns true if any property currently has validation errors.
        /// </summary>
        public bool HasErrors => _errors.Any(kvp => kvp.Value.Count > 0);

        /// <summary>
        /// Gets all validation error messages for a specific property.
        /// If the property has no errors, returns an empty enumerable.
        /// </summary>
        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return Enumerable.Empty<string>();

            return _errors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<string>();
        }

        /// <summary>
        /// Adds a validation error to the specified property.
        /// </summary>
        protected void AddError(string errorMessage, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                return;

            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(errorMessage))
            {
                _errors[propertyName].Add(errorMessage);
                RaiseErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// Clears all validation errors for the specified property.
        /// </summary>
        protected void ClearErrors([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                return;

            if (_errors.ContainsKey(propertyName) && _errors[propertyName].Count > 0)
            {
                _errors[propertyName].Clear();
                RaiseErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// Notifies WPF that validation errors have changed for this property.
        /// This triggers red borders and other validation UI feedback.
        /// </summary>
        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Helper method to validate that a string represents a valid decimal number.
        /// Clears errors if valid, adds error if not.
        /// </summary>
        protected bool ValidateNumeric(string value, [CallerMemberName] string propertyName = null)
        {
            // Empty or null is valid (field is optional)
            if (string.IsNullOrWhiteSpace(value))
            {
                ClearErrors(propertyName);
                return true;
            }

            // Try to parse as float
            if (float.TryParse(value, out _))
            {
                ClearErrors(propertyName);
                return true;
            }

            // Parse failed
            AddError($"'{value}' is not a valid number.", propertyName);
            return false;
        }
    }
}
