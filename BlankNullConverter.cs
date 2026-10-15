using System;
using System.Globalization;
using System.Windows.Data;

namespace RegistryKey_Check
{
    public class BlankNullConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string text ? ViewModels.Shown(text) : value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
