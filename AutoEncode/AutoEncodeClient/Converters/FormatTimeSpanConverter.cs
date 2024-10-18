using AutoEncodeUtilities;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoEncodeClient.Converters;

public class FormatTimeSpanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            return HelperMethods.FormatEncodingTime(timeSpan);
        }

        return default(TimeSpan);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
