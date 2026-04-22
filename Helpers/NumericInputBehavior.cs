// Purpose: Attached behavior that restricts TextBox input to valid decimal numbers.
//
// Features:
//   • Allows digits 0-9 and decimal point (.)
//   • Allows leading minus sign (-) for negative numbers
//   • Prevents multiple decimal points
//   • Validates pasted content and rejects invalid pastes
//   • Supports partial input states (e.g. ".", "123.", "0.")
//   • Preserves trailing zeros in display
//
// Usage in XAML:
//   <TextBox helpers:NumericInputBehavior.IsNumericOnly="True" ... />

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Linac_QA_Software.Helpers
{
    public static class NumericInputBehavior
    {
        /// <summary>
        /// Attached dependency property to enable numeric-only validation on a TextBox.
        /// </summary>
        public static readonly DependencyProperty IsNumericOnlyProperty =
            DependencyProperty.RegisterAttached(
                "IsNumericOnly",
                typeof(bool),
                typeof(NumericInputBehavior),
                new PropertyMetadata(false, OnIsNumericOnlyChanged));

        public static bool GetIsNumericOnly(DependencyObject element)
            => (bool)element.GetValue(IsNumericOnlyProperty);

        public static void SetIsNumericOnly(DependencyObject element, bool value)
            => element.SetValue(IsNumericOnlyProperty, value);

        /// <summary>
        /// Called when IsNumericOnly is set on a TextBox.
        /// Hooks PreviewTextInput and CommandBinding events for validation.
        /// </summary>
        private static void OnIsNumericOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox) return;

            bool isEnabled = (bool)e.NewValue;

            // Unsubscribe first to prevent duplicate handlers if re-assigned
            textBox.PreviewTextInput -= TextBox_PreviewTextInput;
            textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
            DataObject.RemovePastingHandler(textBox, TextBox_Pasting);

            if (isEnabled)
            {
                textBox.PreviewTextInput += TextBox_PreviewTextInput;
                textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                DataObject.AddPastingHandler(textBox, TextBox_Pasting);
            }
        }

        /// <summary>
        /// Validates each keystroke before the character is added to the TextBox.
        /// Allows: 0-9, ".", and "-" (only at start for negative numbers).
        /// </summary>
        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            // Get what the text WILL look like if we allow this input
            string newText = GetResultingText(textBox, e.Text);

            if (!IsValidNumeric(newText))
                e.Handled = true; // Reject the keystroke
        }

        /// <summary>
        /// Handles special keys like Backspace, Delete, and Copy/Cut/Paste shortcuts.
        /// These need explicit handling because they don't trigger PreviewTextInput.
        /// </summary>
        private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            // Block spacebar (PreviewTextInput doesn't catch it)
            if (e.Key == Key.Space)
            {
                e.Handled = true;
                return;
            }

            // Allow standard navigation/edit keys
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab || e.Key == Key.Enter ||
                e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Home || e.Key == Key.End)
                return;

            // Allow Ctrl+A, C, X, V
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                if (e.Key == Key.A || e.Key == Key.C || e.Key == Key.X || e.Key == Key.V)
                    return;
            }
        }

        /// <summary>
        /// Validates clipboard content before pasting.
        /// Rejects paste operations with invalid numeric content.
        /// </summary>
        private static void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string pastedText = (string)e.DataObject.GetData(DataFormats.Text);
            string resultingText = GetResultingText(textBox, pastedText);

            if (!IsValidNumeric(resultingText))
                e.CancelCommand(); // Reject the paste
        }

        /// <summary>
        /// Simulates what the TextBox content would be if the new text is inserted
        /// at the current cursor position.
        /// </summary>
        private static string GetResultingText(TextBox textBox, string newText)
        {
            string currentText = textBox.Text ?? "";
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            return currentText.Remove(selectionStart, selectionLength)
                              .Insert(selectionStart, newText);
        }

        /// <summary>
        /// Validates that the text represents a valid decimal number format.
        /// 
        /// Valid formats:
        ///   - "5" (integer)
        ///   - "5.0" (decimal with trailing zero)
        ///   - "." (leading decimal point, will become "0.")
        ///   - "5." (integer with trailing decimal point)
        ///   - "-5" (negative integer)
        ///   - "-5.123" (negative decimal)
        ///   - "" (empty string, user is clearing field)
        ///
        /// Invalid formats:
        ///   - "5.5.5" (multiple decimal points)
        ///   - "5a" (non-numeric characters)
        ///   - "--5" (multiple minus signs)
        ///   - "5-" (minus sign not at start)
        /// </summary>
        private static bool IsValidNumeric(string text)
        {
            // Empty string is valid (user is clearing the field)
            if (string.IsNullOrEmpty(text))
                return true;

            // Just a minus sign is valid (user is typing negative number)
            if (text == "-")
                return true;

            // Just a decimal point is valid (user will type digits after)
            if (text == ".")
                return true;

            // Minus followed by decimal is valid (will become "-0.")
            if (text == "-.")
                return true;

            // Check for invalid characters
            foreach (char c in text)
            {
                if (!char.IsDigit(c) && c != '.' && c != '-')
                    return false; // Non-numeric character found
            }

            // Check for multiple decimal points
            if (text.Count(c => c == '.') > 1)
                return false;

            // Check for multiple minus signs or minus not at start
            int minusCount = text.Count(c => c == '-');
            if (minusCount > 1)
                return false;
            if (minusCount == 1 && text[0] != '-')
                return false; // Minus sign not at the beginning

            // If it ends with a dot, we've already validated the structure above, 
            // so we skip the TryParse (which would fail on "5.")
            if (text.EndsWith("."))
            {
                string partBeforeDot = text.TrimEnd('.');
                if (string.IsNullOrEmpty(partBeforeDot) || partBeforeDot == "-") return true;
                return float.TryParse(partBeforeDot, out _);
            }

            // Attempt to parse as float to ensure it's within valid numeric range
            // (This catches edge cases like "999999999999999999" that overflow)
            if (!float.TryParse(text, out _))
                return false;

            return true;
        }
    }
}
