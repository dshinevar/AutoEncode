using AutoEncodeUtilities.Enums;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoEncodeClient.Converters;

public class HDRFlagToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Visibility visibility = Visibility.Collapsed;
        if (value is HDRFlags flags)
        {
            if (parameter is HDRFlags flag)
            {
                visibility = flags.HasFlag(flag) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        return visibility;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
