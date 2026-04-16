// Purpose: A minimal ICommand implementation that wraps an Action delegate.
// Used to bind buttons and other controls in XAML to methods in a ViewModel
// without any code-behind.

using System;
using System.Windows.Input;

namespace Linac_QA_Software.Helpers
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>Creates a command that is always enabled.</summary>
        public RelayCommand(Action execute) : this(execute, canExecute: null) { }

        /// <summary>
        /// Creates a command that is only enabled when <paramref name="canExecute"/> returns true.
        /// </summary>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Wired to WPF's CommandManager so the button enabled-state is
        /// re-evaluated automatically whenever UI interaction occurs.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
        public void Execute(object parameter) => _execute();
    }
}