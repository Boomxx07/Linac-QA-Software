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
    /// <summary>
    /// Attached property that bridges a WPF ListBox's SelectedItems collection to a ViewModel property.
    /// Since ListBox.SelectedItems is not a DependencyProperty, this helper enables two-way-like binding.
    /// </summary>
    public static class ListBoxHelper
    {
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

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox listBox) return;

            // Clean up old event subscription to prevent memory leaks
            listBox.SelectionChanged -= ListBox_SelectionChanged;

            if (e.NewValue is IList)
            {
                listBox.SelectionChanged += ListBox_SelectionChanged;
            }
        }

        /// <summary>
        /// Syncs the View selection changes into the ViewModel's collection.
        /// </summary>
        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox) return;

            var boundList = GetSelectedItems(listBox);
            if (boundList == null) return;

            // 1. Remove items that were unselected in the UI
            foreach (var item in e.RemovedItems)
            {
                if (boundList.Contains(item))
                {
                    boundList.Remove(item);
                }
            }

            // 2. Add items that were newly selected in the UI
            foreach (var item in e.AddedItems)
            {
                if (!boundList.Contains(item))
                {
                    boundList.Add(item);
                }
            }
        }
    }
}