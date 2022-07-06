using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AutomatedFFmpegClient.Converters
{
    public class EncodedColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush brush = new SolidColorBrush();
            bool isEncoded = (bool)value;
            brush.Color = isEncoded ? Colors.Green : Colors.Red;
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
