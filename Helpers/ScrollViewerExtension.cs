using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Linac_QA_Software.Helpers
{
    public static class ScrollViewerExtension
    {
        public static bool GetBubbleMouseWheel(DependencyObject obj)
            => (bool)obj.GetValue(BubbleMouseWheelProperty);

        public static void SetBubbleMouseWheel(DependencyObject obj, bool value)
            => obj.SetValue(BubbleMouseWheelProperty, value);

        public static readonly DependencyProperty BubbleMouseWheelProperty =
            DependencyProperty.RegisterAttached(
                "BubbleMouseWheel",
                typeof(bool),
                typeof(ScrollViewerExtension),
                new UIPropertyMetadata(false, OnBubbleMouseWheelChanged));

        private static void OnBubbleMouseWheelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                    element.PreviewMouseWheel += Element_PreviewMouseWheel;
                else
                    element.PreviewMouseWheel -= Element_PreviewMouseWheel;
            }
        }

        private static void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var parent = (DependencyObject)sender;

            while (parent != null && parent is not ScrollViewer)
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is ScrollViewer scroll)
            {
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }

}
