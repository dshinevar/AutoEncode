using AutoEncodeUtilities.Data;
using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace AutoEncodeClient.Converters;

public class DolbyVisionInfoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DolbyVisionInfo dvInfo)
        {
            StringBuilder sbDVInfo = new();
            sbDVInfo.Append($"v{dvInfo.Version}, Profile {dvInfo.Profile}, ");

            if (dvInfo.BLPresent is true)
                sbDVInfo.Append("BL+");
            if (dvInfo.ELPresent is true)
                sbDVInfo.Append("EL+");
            if (dvInfo.RPUPresent is true)
                sbDVInfo.Append("RPU");

            return sbDVInfo.ToString().TrimEnd('+', ' ');
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
