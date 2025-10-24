using System;
using DesktopTaskAid.Models;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class ModelTests
    {
        [Test]
        public void AppSettings_Defaults()
        {
            var settings = new AppSettings();
            Assert.AreEqual("light", settings.Theme);
            Assert.IsFalse(settings.HelperEnabled);
        }

        [Test]
        public void AppState_Defaults()
        {
            var state = new AppState();
            Assert.IsNotNull(state.Tasks);
            Assert.IsNotNull(state.Settings);
            Assert.IsNotNull(state.Calendar);
            Assert.IsNotNull(state.Timer);
            Assert.AreEqual(1, state.CurrentPage);
            Assert.AreEqual(10, state.PageSize);
        }

        [Test]
        public void CalendarState_Defaults()
        {
            var calendar = new CalendarState();
            Assert.AreEqual(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), calendar.CurrentMonth);
            Assert.AreEqual(DateTime.Today, calendar.SelectedDate);
        }

        [Test]
        public void TimerState_ResetAndRefresh()
        {
            var timer = new TimerState
            {
                RemainingSeconds = 10,
                IsRunning = true,
                DoneTodaySeconds = 100,
                DoneTodayDate = DateTime.Today.AddDays(-1)
            };

            timer.Reset();
            Assert.AreEqual(timer.DurationSeconds, timer.RemainingSeconds);
            Assert.IsFalse(timer.IsRunning);

            timer.RefreshDailyTracking();
            Assert.AreEqual(DateTime.Today, timer.DoneTodayDate);
            Assert.AreEqual(0, timer.DoneTodaySeconds);
        }

        [Test]
        public void TaskItem_DefaultValues()
        {
            var task = new TaskItem();
            Assert.IsNotNull(task.Id);
            Assert.AreEqual("none", task.ReminderStatus);
            Assert.AreEqual("Not set", task.ReminderLabel);
            Assert.LessOrEqual((DateTime.Now - task.CreatedAt).TotalSeconds, 1);
        }

        [Test]
        public void TaskItem_GetFullDueDateTime_ComposesDateAndTime()
        {
            var date = DateTime.Today;
            var task = new TaskItem
            {
                DueDate = date,
                DueTime = TimeSpan.FromHours(2)
            };

            Assert.AreEqual(date.AddHours(2), task.GetFullDueDateTime());
        }

        [Test]
        public void TaskItem_IsOverdue_ReturnsTrueForPast()
        {
            var task = new TaskItem
            {
                DueDate = DateTime.Now.AddHours(-1),
                DueTime = TimeSpan.Zero
            };

            Assert.IsTrue(task.IsOverdue());
        }
    }
}
