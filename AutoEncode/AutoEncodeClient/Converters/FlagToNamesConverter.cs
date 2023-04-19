using AutoEncodeUtilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AutoEncodeClient.Converters
{
    public class FlagToNamesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string flagString = string.Empty;
            if (value is Enum enumeration)
            {
                if (enumeration.GetType().IsDefined(typeof(FlagsAttribute), false) is true)
                {
                    IEnumerable<Enum> flags = enumeration.GetFlags();
                    return string.Join(", ", flags.Select(x => x.GetDisplayName()));
                }
            }

            return flagString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
