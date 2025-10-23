using System;
using System.Globalization;
using System.Windows.Data;
using DesktopTaskAid.ViewModels;

namespace DesktopTaskAid.Converters
{
    public class CalendarDayTagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CalendarDay calendarDay)
            {
                var propertyName = parameter as string;
                
                if (propertyName == "IsSelected")
                {
                    return calendarDay.IsSelected;
                }
                else if (propertyName == "IsToday")
                {
                    return calendarDay.IsToday;
                }
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
