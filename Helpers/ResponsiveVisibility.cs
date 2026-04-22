using System.Windows;

namespace Linac_QA_Software.Helpers
{
    public static class ResponsiveVisibility
    {
        public static readonly DependencyProperty MinWidthProperty =
            DependencyProperty.RegisterAttached(
                "MinWidth",
                typeof(double),
                typeof(ResponsiveVisibility),
                new PropertyMetadata(double.NaN, OnMinWidthChanged));

        public static double GetMinWidth(DependencyObject obj) => (double)obj.GetValue(MinWidthProperty);
        public static void SetMinWidth(DependencyObject obj, double value) => obj.SetValue(MinWidthProperty, value);

        private static void OnMinWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element) return;

            // Use the Loaded event to find the parent window and hook into size changes
            element.Loaded += (s, _) =>
            {
                Window window = Window.GetWindow(element);
                if (window == null) return;

                // Define the handler so we can unsubscribe later
                SizeChangedEventHandler handler = (sender, args) => UpdateVisibility(element);

                window.SizeChanged += handler;
                UpdateVisibility(element); // Initial check

                // Unsubscribe when the element is removed from the UI to prevent memory leaks
                element.Unloaded += (us, ue) =>
                {
                    window.SizeChanged -= handler;
                };
            };
        }

        private static void UpdateVisibility(FrameworkElement element)
        {
            double minWidth = GetMinWidth(element);
            if (double.IsNaN(minWidth)) return;

            Window window = Window.GetWindow(element);
            if (window == null) return;

            // If the window is too narrow, hide the element
            element.Visibility = window.ActualWidth < minWidth
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}