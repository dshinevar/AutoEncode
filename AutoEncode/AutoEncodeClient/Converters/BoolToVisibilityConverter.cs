using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoEncodeClient.Converters
{
    /// <summary>
    /// Converts bool to <see cref="Visibility"/>
    /// <para/>
    /// True to <see cref="Visibility.Visible"/>
    /// <para/>
    /// False to <see cref="Visibility.Collapsed"/>
    /// </summary>
    /// <see cref="BoolToInvisibilityConverter"/>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = Visibility.Collapsed;
            if (value is bool visible)
            {
                visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }

            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
