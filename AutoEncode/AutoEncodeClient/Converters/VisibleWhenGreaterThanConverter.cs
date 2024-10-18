using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoEncodeClient.Converters;

public class VisibleWhenGreaterThanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Visibility visibility = Visibility.Collapsed;
        try
        {
            int valueInt = System.Convert.ToInt32(value);
            int parameterInt = System.Convert.ToInt32(parameter);

            visibility = valueInt > parameterInt ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        return visibility;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
