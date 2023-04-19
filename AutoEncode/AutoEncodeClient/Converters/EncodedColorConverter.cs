using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoEncodeClient.Converters
{
    public class EncodedColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color color = Colors.Black;
            if (value is bool encoded)
            {
                color = encoded ? Colors.ForestGreen : Colors.Red;
            }

            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
