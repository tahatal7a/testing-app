using System;
using System.Globalization;
using System.Windows.Data;

namespace DesktopTaskAid.Converters
{
    public class TimeFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                var hour = timeSpan.Hours;
                var minute = timeSpan.Minutes;
                var period = hour >= 12 ? "PM" : "AM";
                var hour12 = hour % 12 == 0 ? 12 : hour % 12;
                
                if (minute == 0)
                    return $"{hour12} {period}";
                else
                    return $"{hour12}:{minute:D2} {period}";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
