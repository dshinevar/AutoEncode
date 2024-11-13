using AutoEncodeUtilities.Enums;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoEncodeClient.Converters;

public class SourceFileEncodingStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Color color = Colors.Black;

        if (value is SourceFileEncodingStatus encodingStatus)
        {
            color = encodingStatus switch
            {
                SourceFileEncodingStatus.ENCODED => Colors.SeaGreen,
                SourceFileEncodingStatus.IN_QUEUE => Colors.RoyalBlue,
                _ => Colors.Black,
            };
        }

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
