using System;
using System.Globalization;
using System.Windows.Data;
using DesktopTaskAid.Services;

namespace DesktopTaskAid.Converters
{
    public class SecondsToDurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is int seconds)
                {
                    var hours = seconds / 3600;
                    var mins = (seconds % 3600) / 60;
                    var secs = seconds % 60;
                    return $"{hours}:{mins:D2}:{secs:D2}";
                }
                return "0:00:00";
            }
            catch (Exception ex)
            {
                LoggingService.LogError("ERROR in SecondsToDurationConverter.Convert", ex);
                return "0:00:00";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
