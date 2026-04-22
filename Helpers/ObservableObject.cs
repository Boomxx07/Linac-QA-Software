// Purpose: Base class for all ViewModels.
// Provides a standard INotifyPropertyChanged implementation so WPF data
// bindings automatically update when properties change.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Linac_QA_Software.Helpers
{
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Notifies the UI that a property value has changed.
        /// The [CallerMemberName] attribute automatically fills in the calling
        /// property's name, so you rarely need to pass it explicitly.
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;

                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    // If we are on a background thread, invoke on the UI thread
                    dispatcher.BeginInvoke(new Action(() =>
                        handler.Invoke(this, new PropertyChangedEventArgs(propertyName))));
                }
                else
                {
                    // Already on the UI thread
                    handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        /// <summary>
        /// Sets the backing field to a new value and fires PropertyChanged,
        /// but ONLY if the value has actually changed.  Returns true when changed.
        /// </summary>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            // Note the EqualityComparer<T> avoids boxing (i.e. if a value type is used, it won't be boxed to object for comparison).  This is more
            // efficient than using object.Equals() or EqualityComparer.Default.Equals() which would box (wrap inside an object) value types.
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}