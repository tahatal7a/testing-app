using System;
using System.Globalization;
using System.Windows.Data;

namespace DesktopTaskAid.Converters
{
    public class DateFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("dd-MM-yyyy");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && DateTime.TryParseExact(str, "dd-MM-yyyy", culture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            return null;
        }
    }
}
