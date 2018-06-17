using System;
using System.Globalization;
using System.Windows.Data;

namespace Eurydice.Windows.App.Controls.Converters
{
    /// <summary>
    ///     Calculates inner radius for Sunburst Chart slice.
    /// </summary>
    internal sealed class InnerRadiusCalculator : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var level = (int) values[1];
            var sliceWidth = (double) values[0];
            return sliceWidth * level;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}