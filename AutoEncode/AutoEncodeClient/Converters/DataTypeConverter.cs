﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoEncodeClient.Converters;

public class DataTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value?.GetType() ?? Binding.DoNothing;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
