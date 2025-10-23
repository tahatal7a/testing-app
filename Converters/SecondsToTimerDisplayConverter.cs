using System;
using System.Globalization;
using System.Windows.Data;
using DesktopTaskAid.Services;

namespace DesktopTaskAid.Converters
{
    public class SecondsToTimerDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is int seconds)
                {
                    var mins = seconds / 60;
                    var secs = seconds % 60;
                    return $"{mins:D2}:{secs:D2}";
                }
                return "00:00";
            }
            catch (Exception ex)
            {
                LoggingService.LogError("ERROR in SecondsToTimerDisplayConverter.Convert", ex);
                return "00:00";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
