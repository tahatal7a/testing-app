using System;

namespace DesktopTaskAid.Models
{
    public class TimerState
    {
        public int DurationSeconds { get; set; }
        public int RemainingSeconds { get; set; }
        public bool IsRunning { get; set; }
        public int DoneTodaySeconds { get; set; }
        public DateTime DoneTodayDate { get; set; }

        public TimerState()
        {
            DurationSeconds = 25 * 60; // 25 minutes
            RemainingSeconds = 25 * 60;
            IsRunning = false;
            DoneTodaySeconds = 0;
            DoneTodayDate = DateTime.Today;
        }

        public void Reset()
        {
            RemainingSeconds = DurationSeconds;
            IsRunning = false;
        }

        public void RefreshDailyTracking()
        {
            if (DoneTodayDate.Date != DateTime.Today)
            {
                DoneTodayDate = DateTime.Today;
                DoneTodaySeconds = 0;
            }
        }
    }
}
