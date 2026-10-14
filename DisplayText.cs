using System;
using System.Globalization;
using System.Windows.Data;

namespace RegistryKey_Check
{
    public class DisplayTextConverter : IValueConverter
    {
        public static string Display(string text, bool valueName)
        {
            if (text == "This_field_is_NULL")
            {
                return string.Empty;
            }
            if (valueName && text.Length == 0)
            {
                return "(Default)";
            }
            return text;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Display(value as string ?? string.Empty, parameter as string == "name");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
