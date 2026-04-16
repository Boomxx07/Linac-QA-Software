// Purpose: Attached property that bridges a WPF ListBox's SelectedItems
// collection to a ViewModel property.
//
// Background: WPF's built-in ListBox.SelectedItems is not a DependencyProperty,
// so it cannot be used with standard {Binding}.  This helper works around that
// by attaching a custom property and syncing it on SelectionChanged.
//
// Usage in XAML:
//   <ListBox helpers:ListBoxHelper.SelectedItems="{Binding SelectedPhysicists}" ... />

using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Linac_QA_Software.Helpers
{
    public static class ListBoxHelper
    {
        // Registers the attached dependency property with WPF's property system.
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(IList),
                typeof(ListBoxHelper),
                new PropertyMetadata(null, OnSelectedItemsChanged));

        public static void SetSelectedItems(DependencyObject element, IList value)
            => element.SetValue(SelectedItemsProperty, value);

        public static IList GetSelectedItems(DependencyObject element)
            => (IList)element.GetValue(SelectedItemsProperty);

        /// <summary>
        /// Called once when the attached property is first set on a ListBox.
        /// Hooks the SelectionChanged event so we can keep the bound list in sync.
        /// </summary>
        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox listBox) return;

            // Unsubscribe before subscribing to prevent duplicate handlers
            // if the property is ever re-assigned at runtime.
            listBox.SelectionChanged -= ListBox_SelectionChanged;
            listBox.SelectionChanged += ListBox_SelectionChanged;
        }

        /// <summary>
        /// Mirrors the ListBox's current selection into the bound ViewModel collection.
        /// </summary>
        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox) return;

            var boundList = GetSelectedItems(listBox);
            if (boundList == null) return;

            // Rebuild the bound list from scratch on every change.
            boundList.Clear();
            foreach (var item in listBox.SelectedItems)
                boundList.Add(item);
        }
    }
}