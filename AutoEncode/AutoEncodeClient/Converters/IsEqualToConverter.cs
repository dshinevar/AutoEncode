using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoEncodeClient.Converters
{
    public class IsEqualToConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => string.Equals($"{value}", $"{parameter}", StringComparison.InvariantCultureIgnoreCase);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue == true)
            {
                return parameter;
            }
            else
            {
                return Binding.DoNothing;
            }
        }
    }
}
