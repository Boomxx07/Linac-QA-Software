using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Linac_QA_Software.Helpers
{
    public static class ResponsiveVisibility
    {
        public static double GetMinWidth(DependencyObject obj)
            => (double)obj.GetValue(MinWidthProperty);

        public static void SetMinWidth(DependencyObject obj, double value)
            => obj.SetValue(MinWidthProperty, value);

        public static readonly DependencyProperty MinWidthProperty =
            DependencyProperty.RegisterAttached(
                "MinWidth",
                typeof(double),
                typeof(ResponsiveVisibility),
                new PropertyMetadata(double.NaN, OnMinWidthChanged));

        private static void OnMinWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                element.Loaded += (_, __) => UpdateVisibility(element);
                Window window = Window.GetWindow(element);
                if (window != null)
                    window.SizeChanged += (_, __) => UpdateVisibility(element);
            }
        }

        private static void UpdateVisibility(FrameworkElement element)
        {
            double minWidth = GetMinWidth(element);
            Window window = Window.GetWindow(element);

            if (window == null)
                return;

            element.Visibility = window.ActualWidth < minWidth
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

}
