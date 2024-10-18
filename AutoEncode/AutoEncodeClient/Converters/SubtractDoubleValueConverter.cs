using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoEncodeClient.Converters;

public class SubtractDoubleValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double startValue && parameter is not null)
        {
            double paramDouble;
            double returnVal;
            try
            {
                paramDouble = System.Convert.ToDouble(parameter);
                returnVal = startValue - paramDouble;
            }
            catch
            {
                return value;
            }

            return returnVal < 0 ? 0 : returnVal;
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
