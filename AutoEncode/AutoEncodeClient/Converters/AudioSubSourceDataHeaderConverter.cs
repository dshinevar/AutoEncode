using AutoEncodeUtilities.Data;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoEncodeClient.Converters
{
    public class AudioSubSourceDataHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string headerInfo = string.Empty;
            if (value is AudioStreamData audioStreamData)
            {
                headerInfo = $"({audioStreamData.CodecName} - {audioStreamData.Language})";
            }
            else if (value is SubtitleStreamData subtitleStreamData)
            {
                headerInfo = $"({subtitleStreamData.Language}{(subtitleStreamData.Forced is true ? " forced" : string.Empty)})";
            }

            return headerInfo;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
