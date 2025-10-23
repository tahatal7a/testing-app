using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DesktopTaskAid.Services;

namespace DesktopTaskAid.Converters
{
    public class ReminderStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string status)
                {
                    switch (status.ToLower())
                    {
                        case "active":
                            return new SolidColorBrush(Color.FromRgb(180, 223, 210)); // #B4DFD2
                        case "overdue":
                            return new SolidColorBrush(Color.FromRgb(255, 194, 181)); // #FFC2B5
                        case "none":
                        default:
                            return new SolidColorBrush(Color.FromRgb(255, 238, 181)); // #FFEEB5
                    }
                }
                return new SolidColorBrush(Color.FromRgb(255, 238, 181));
            }
            catch (Exception ex)
            {
                LoggingService.LogError("ERROR in ReminderStatusToBrushConverter.Convert", ex);
                return new SolidColorBrush(Color.FromRgb(255, 238, 181)); // Default color
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
