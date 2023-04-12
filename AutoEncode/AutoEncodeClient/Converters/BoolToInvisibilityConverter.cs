using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoEncodeClient.Converters
{
    /// <summary>
    /// Converts bool to <see cref="Visibility"/>
    /// <para/>
    /// True to <see cref="Visibility.Collapsed"/>
    /// <para/>
    /// False to <see cref="Visibility.Visible"/>
    /// </summary>
    /// <see cref="BoolToVisibilityConverter"/>
    public class BoolToInvisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = Visibility.Visible;
            if (value is bool invisible)
            {
                visibility = invisible ? Visibility.Collapsed : Visibility.Visible;
            }

            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
