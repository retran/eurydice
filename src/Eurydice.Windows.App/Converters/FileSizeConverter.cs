using System;
using System.Globalization;
using System.Windows.Data;

namespace Eurydice.Windows.App.Converters
{
    /// <summary>
    ///     Converts file size to human readable string.
    /// </summary>
    internal sealed class FileSizeConverter : IValueConverter
    {
        private static readonly long[] Bounds =
        {
            0x1000000000000000,
            0x4000000000000,
            0x10000000000,
            0x40000000,
            0x100000,
            0x400
        };

        private static readonly string[] Suffixes = {"EB", "PB", "TB", "GB", "MB", "KB", "B"};

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var size = (long) value;

            if (size == 0) return "0 B";

            var i = 0;
            while (size < Bounds[i]) i++;
            var val = (double) (size >> ((5 - i) * 10)) / 1024;
            return val.ToString("0.## ") + Suffixes[i];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}