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
            // Prefer an explicit resource flag if present (easier to unit test and more robust)
            try
            {
                if (Application.Current != null && Application.Current.Resources.Contains("IsDarkTheme"))
                {
                    var obj = Application.Current.Resources["IsDarkTheme"];
                    bool? flag = null;
                    if (obj is bool b)
                    {
                        flag = b;
                    }
                    else if (obj is bool?)
                    {
                        flag = (bool?)obj;
                    }

                    if (flag.HasValue)
                    {
                        return flag.Value ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
                    }
                }
            }
            catch
            {
                // ignore and fallback to dictionary-based detection
            }

            // Fallback: Try reading a simple string theme name resource set by view model
            try
            {
                if (Application.Current != null && Application.Current.Resources.Contains("ThemeName"))
                {
                    var themeName = Application.Current.Resources["ThemeName"] as string;
                    if (!string.IsNullOrWhiteSpace(themeName))
                    {
                        return string.Equals(themeName, "dark", StringComparison.OrdinalIgnoreCase)
                            ? Brushes.White
                            : new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
                    }
                }
            }
            catch { }

            // Fallback: Check merged dictionaries name contains "dark" (case-insensitive)
            var isDark = false;
            try
            {
                var dictionaries = Application.Current?.Resources?.MergedDictionaries;
                if (dictionaries != null && dictionaries.Count > 0)
                {
                    foreach (var dict in dictionaries)
                    {
                        var src = dict?.Source?.ToString() ?? string.Empty;
                        if (src.IndexOf("dark", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            isDark = true;
                            break;
                        }
                    }
                }
            }
            catch { }

            // Return white for dark theme, dark gray for light theme
            return isDark ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
