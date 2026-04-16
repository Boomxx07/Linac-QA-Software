// Purpose: Base class for all ViewModels.
// Provides a standard INotifyPropertyChanged implementation so WPF data
// bindings automatically update when properties change.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Linac_QA_Software.Helpers
{
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies the UI that a property value has changed.
        /// The [CallerMemberName] attribute automatically fills in the calling
        /// property's name, so you rarely need to pass it explicitly.
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Sets the backing field to a new value and fires PropertyChanged,
        /// but ONLY if the value has actually changed.  Returns true when changed.
        /// </summary>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}