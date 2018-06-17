using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Eurydice.Windows.App.Controls.Converters
{
    /// <summary>
    ///     Calculates origin point for Sunburst Chart slice.
    /// </summary>
    internal sealed class OriginCalculator : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var width = (double) values[0];
            var height = (double) values[1];

            return new Point(width / 2, height / 2);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}