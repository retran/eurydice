using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Eurydice.Windows.App.Converters
{
    /// <summary>
    ///     Determines visibility for controls visible only when pipeline is running.
    /// </summary>
    internal sealed class RunningToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var running = (bool) value;
            return running ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}