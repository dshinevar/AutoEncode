using AutoEncodeUtilities;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoEncodeClient.Converters
{
    public class SecondsToTimestampConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int numSeconds)
            {
                return HelperMethods.ConvertSecondsToTimestamp(numSeconds);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
