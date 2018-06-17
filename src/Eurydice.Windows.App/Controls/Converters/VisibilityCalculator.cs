using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Eurydice.Windows.App.Controls.Converters
{
    /// <summary>
    ///     Determines visibility of Sunburst Chart Item based on depth and item level.
    /// </summary>
    internal sealed class VisibilityCalculator : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == DependencyProperty.UnsetValue) return Visibility.Hidden;

            var depth = (int) values[0];
            var level = (int) values[1];

            return level < depth
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}