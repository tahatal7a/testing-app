using System;

namespace DesktopTaskAid.Models
{
    public class CalendarState
    {
        public DateTime CurrentMonth { get; set; }
        public DateTime SelectedDate { get; set; }

        public CalendarState()
        {
            CurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            SelectedDate = DateTime.Today;
        }
    }
}
