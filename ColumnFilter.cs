using System;
using System.Windows;

namespace RegistryKey_Check
{
    public static class ColumnFilter
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached("Text", typeof(string), typeof(ColumnFilter), new PropertyMetadata(string.Empty, OnTextChanged));

        public static event EventHandler? Changed;

        public static string GetText(DependencyObject element)
        {
            return (string)element.GetValue(TextProperty);
        }

        public static void SetText(DependencyObject element, string value)
        {
            element.SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            Changed?.Invoke(element, EventArgs.Empty);
        }
    }
}
