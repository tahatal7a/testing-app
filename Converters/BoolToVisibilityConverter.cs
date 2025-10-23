using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DesktopTaskAid.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Support "Inverse" parameter to invert the logic
                bool isInverse = parameter != null && parameter.ToString().Equals("Inverse", StringComparison.OrdinalIgnoreCase);
                
                if (isInverse)
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}
