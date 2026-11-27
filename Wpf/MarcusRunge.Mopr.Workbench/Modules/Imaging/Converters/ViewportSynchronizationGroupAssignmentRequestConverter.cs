using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Converters
{
    public sealed class ViewportSynchronizationGroupAssignmentRequestConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => values.Length == 2 && values[0] is string viewportId && !string.IsNullOrWhiteSpace(viewportId) && values[1] is Guid groupId && groupId != Guid.Empty ? new ViewportSynchronizationGroupAssignmentRequest(viewportId, groupId) : DependencyProperty.UnsetValue;

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}