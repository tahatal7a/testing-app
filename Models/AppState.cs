using System;
using System.Collections.Generic;

namespace DesktopTaskAid.Models
{
    public class AppState
    {
        public List<TaskItem> Tasks { get; set; }
        public AppSettings Settings { get; set; }
        public CalendarState Calendar { get; set; }
        public TimerState Timer { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }

        public AppState()
        {
            Tasks = new List<TaskItem>();
            Settings = new AppSettings();
            Calendar = new CalendarState();
            Timer = new TimerState();
            CurrentPage = 1;
            PageSize = 10;
        }
    }
}
