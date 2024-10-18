using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoEncodeClient.Converters;

public class IsNullVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
