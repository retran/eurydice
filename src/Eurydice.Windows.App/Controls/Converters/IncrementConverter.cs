using System;
using System.Globalization;
using System.Windows.Data;

namespace Eurydice.Windows.App.Controls.Converters
{
    /// <summary>
    ///     Increments integer value.
    /// </summary>
    internal sealed class IncrementConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int) value + 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}