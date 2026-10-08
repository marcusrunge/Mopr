using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.Converters
{
    /// <summary>
    /// Converts false to Visible and true to Collapsed.
    /// </summary>
    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is false ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
