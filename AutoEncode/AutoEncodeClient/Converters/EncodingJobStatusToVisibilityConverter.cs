using AutoEncodeUtilities.Enums;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoEncodeClient.Converters
{
    public class EncodingJobStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EncodingJobStatus status)
            {
                if (parameter is EncodingJobStatus maxStatus)
                {
                    return (EncodingJobStatus.ENCODED <= status && status <= maxStatus) ? Visibility.Visible : Visibility.Collapsed;
                }

                return status >= EncodingJobStatus.ENCODING ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
