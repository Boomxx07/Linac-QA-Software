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
            if (d is not UIElement element) return;

            // Ensure we don't double-subscribe
            element.PreviewMouseWheel -= Element_PreviewMouseWheel;

            if ((bool)e.NewValue)
            {
                element.PreviewMouseWheel += Element_PreviewMouseWheel;
            }
        }

        private static void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled && sender is DependencyObject d)
            {
                // Find the ScrollViewer that contains the element
                var scrollViewer = FindParent<ScrollViewer>(d);

                if (scrollViewer != null)
                {
                    // e.Delta is usually 120. We divide by 3 (or similar) 
                    // to match standard Windows scroll speed.
                    double newOffset = scrollViewer.VerticalOffset - (e.Delta / 3.0);

                    // Constrain the offset to valid bounds
                    if (newOffset < 0) newOffset = 0;
                    if (newOffset > scrollViewer.ScrollableHeight) newOffset = scrollViewer.ScrollableHeight;

                    scrollViewer.ScrollToVerticalOffset(newOffset);

                    // Mark as handled so the inner control doesn't also scroll
                    e.Handled = true;

                    // Re-raise the event as a scrolled event for the parent if necessary
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent,
                        Source = sender
                    };

                    if (VisualTreeHelper.GetParent(scrollViewer) is UIElement parent)
                    {
                        parent.RaiseEvent(eventArg);
                    }
                }
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;

            return FindParent<T>(parentObject);
        }
    }
}