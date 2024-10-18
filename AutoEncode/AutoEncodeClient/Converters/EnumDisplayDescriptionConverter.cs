using AutoEncodeUtilities;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoEncodeClient.Converters;

public class EnumDisplayDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Enum enumeration)
        {
            return enumeration.GetDescription();
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
