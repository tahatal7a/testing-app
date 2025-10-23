using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DesktopTaskAid.Converters
{
    public class IconStrokeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if current theme is dark
            var isDark = false;
            
            if (Application.Current.Resources.MergedDictionaries.Count > 0)
            {
                var theme = Application.Current.Resources.MergedDictionaries[0];
                if (theme.Source != null && theme.Source.ToString().Contains("darkTheme"))
                {
                    isDark = true;
                }
            }
            
            // Return white for dark theme, black for light theme
            return isDark ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
