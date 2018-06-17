using System;
using System.Globalization;
using System.Windows.Data;

namespace Eurydice.Windows.App.Controls.Converters
{
    /// <summary>
    ///     Calculates Sunburst Chart slice slice width (based on render area size).
    /// </summary>
    internal sealed class SliceWidthCalculator : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var width = (double) values[0];
            var height = (double) values[1];
            double depth = (int) values[2];

            var result = Math.Min(width, height) / 2 / (depth + 1);

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}