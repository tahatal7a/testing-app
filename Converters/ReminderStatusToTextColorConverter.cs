using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DesktopTaskAid.Converters
{
    public class ReminderStatusToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status.ToLower())
                {
                    case "active":
                        return new SolidColorBrush(Color.FromRgb(24, 119, 91)); // #18775B
                    case "overdue":
                        return new SolidColorBrush(Color.FromRgb(212, 48, 41)); // #D43029
                    case "none":
                    default:
                        return new SolidColorBrush(Color.FromRgb(212, 152, 41)); // #D49829
                }
            }
            return new SolidColorBrush(Color.FromRgb(212, 152, 41));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
