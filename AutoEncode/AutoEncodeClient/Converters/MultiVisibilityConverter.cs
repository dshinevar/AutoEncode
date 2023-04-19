using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoEncodeClient.Converters
{
    /// <summary>Takes in multiple <see cref="Visibility"/>s and uses the most hidden (greatest) value.</summary>
    public class MultiVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = Visibility.Visible;

            foreach (object value in values)
            {
                if (value is Visibility vis && vis > visibility)
                {
                    visibility = vis;
                }
            }

            return visibility;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
