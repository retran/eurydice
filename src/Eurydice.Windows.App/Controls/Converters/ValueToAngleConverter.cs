using System;
using System.Globalization;
using System.Windows.Data;

namespace Eurydice.Windows.App.Controls.Converters
{
    /// <summary>
    ///     Converts normilized value to angle for Sunburst Chart Slice.
    /// </summary>
    internal sealed class ValueToAngleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double) value * 2 * Math.PI + Math.PI;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}