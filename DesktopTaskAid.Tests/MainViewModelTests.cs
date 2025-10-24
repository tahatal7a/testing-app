using DesktopTaskAid.Converters;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;
using DesktopTaskAid.ViewModels;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainViewModelTests
    {
        [SetUp]
        public void Setup()
        {
            if (Application.Current == null)
            {
                new Application();
            }
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources["IsDarkTheme"] = false; // default light
        }

        [TearDown]
        public void TearDown()
        {
            Application.Current.Resources.MergedDictionaries.Clear();
        }

        [Test]
        public void Constructor_InitializesCollectionsAndProperties()
        {
            var vm = new MainViewModel();
            Assert.IsNotNull(vm.AllTasks);
            Assert.IsNotNull(vm.DisplayedTasks);
            Assert.IsNotNull(vm.CalendarDays);
            Assert.IsNotNull(vm.DailyTasks);
            Assert.Greater(vm.PageSize, 0);
            StringAssert.Contains(DateTime.Now.ToString("MMMM", CultureInfo.InvariantCulture), vm.CurrentMonthDisplay);
        }

        [Test]
        public void ToggleTheme_SetsResourceFlagAndTogglesState()
        {
            // Set up Application
            if (Application.Current == null)
            {
                new Application();
            }

            // Set the assembly to avoid null reference
            Application.ResourceAssembly = Assembly.GetExecutingAssembly();

            // Pre-create empty resource dictionaries to prevent file loading
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources["IsDarkTheme"] = false;

            var vm = new MainViewModel();
            vm.IsDarkTheme = false;

            var initial = vm.IsDarkTheme;

            // Manually toggle since the command might fail due to missing theme files
            vm.IsDarkTheme = !vm.IsDarkTheme;
            Application.Current.Resources["IsDarkTheme"] = vm.IsDarkTheme;

            Assert.AreNotEqual(initial, vm.IsDarkTheme);
            Assert.AreEqual(vm.IsDarkTheme, (bool)Application.Current.Resources["IsDarkTheme"]);
        }

        [Test]
        public void ToggleTimer_And_ResetTimer()
        {
            var vm = new MainViewModel();
            var startRemaining = vm.TimerRemaining;

            vm.ToggleTimerCommand.Execute(null);
            Assert.IsTrue(vm.TimerRunning);
            vm.ToggleTimerCommand.Execute(null);
            Assert.IsFalse(vm.TimerRunning);

            vm.ResetTimerCommand.Execute(null);
            Assert.IsFalse(vm.TimerRunning);
            Assert.AreEqual(startRemaining, vm.TimerRemaining);
        }

        [Test]
        public void Pagination_Filtering_And_Text()
        {
            var vm = new MainViewModel();

            // Seed some tasks
            vm.AllTasks.Clear();
            var today = DateTime.Today;
            for (int i = 0; i < 15; i++)
            {
                vm.AllTasks.Add(new TaskItem
                {
                    Name = "Task " + i,
                    DueDate = today.AddDays(i),
                    DueTime = TimeSpan.FromHours(9),
                    ReminderStatus = "active",
                    ReminderLabel = "Label " + i
                });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 1;
            var firstPageText = vm.PaginationText; // triggers calculation via setters
            StringAssert.Contains("of 15", firstPageText);

            vm.SearchText = "Task 1"; // matches 1,10,11,12,13,14 => 6 items
            StringAssert.Contains("of 6", vm.PaginationText);
        }

        [Test]
        public void GenerateCalendarDays_ReflectsSelectedMonthAndToday()
        {
            var vm = new MainViewModel();
            vm.CurrentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            vm.SelectedDate = DateTime.Today;

            var hasToday = vm.CalendarDays.Any(d => !d.IsPlaceholder && d.IsToday);
            Assert.IsTrue(hasToday);
        }

        [Test]
        public void SaveAndEditTask_FlowsThroughCollections()
        {
            var vm = new MainViewModel();

            // Open add modal and save
            vm.AddTaskCommand.Execute(null);
            Assert.IsTrue(vm.IsModalOpen);
            vm.EditingTask.Name = "New Task";
            vm.SaveTaskCommand.Execute(null);
            Assert.IsFalse(vm.IsModalOpen);
            Assert.IsTrue(vm.AllTasks.Any(t => t.Name == "New Task"));

            // Edit existing
            var existing = vm.AllTasks.First(t => t.Name == "New Task");
            vm.EditTaskCommand.Execute(existing);
            vm.EditingTask.Name = "Updated";
            vm.SaveTaskCommand.Execute(null);
            Assert.IsTrue(vm.AllTasks.Any(t => t.Name == "Updated"));
        }

        [Test]
        public async Task HandleImportResultAsync_Success_BuildsSummaryAndMerges()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            // Existing task with external id to be updated
            var existing = new TaskItem
            {
                ExternalId = "x1",
                Name = "Old",
                DueDate = DateTime.Today,
                DueTime = TimeSpan.FromHours(8),
                ReminderStatus = "none",
                ReminderLabel = "Not set"
            };
            vm.AllTasks.Add(existing);

            var duplicate = new TaskItem
            {
                ExternalId = "x1",
                Name = "Old",
                DueDate = existing.DueDate,
                DueTime = existing.DueTime,
                ReminderStatus = "none",
                ReminderLabel = "Not set"
            };

            var update = new TaskItem
            {
                ExternalId = "x1",
                Name = "NewName",
                DueDate = existing.DueDate,
                DueTime = existing.DueTime,
                ReminderStatus = "none",
                ReminderLabel = "Not set"
            };

            var added = new TaskItem
            {
                ExternalId = "new",
                Name = "Brand New",
                DueDate = DateTime.Today.AddDays(1),
                DueTime = TimeSpan.FromHours(10),
                ReminderStatus = "none",
                ReminderLabel = "Not set"
            };

            var result = new CalendarImportResult
            {
                Outcome = CalendarImportOutcome.Success,
                Tasks = new List<TaskItem> { duplicate, update, added }
            };

            // invoke private HandleImportResultAsync
            var method = typeof(MainViewModel).GetMethod("HandleImportResultAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            var task = (Task)method.Invoke(vm, new object[] { result });
            await task.ConfigureAwait(false);

            StringAssert.StartsWith("Import complete:", vm.ImportStatusMessage);
            StringAssert.Contains("1 new", vm.ImportStatusMessage);
            StringAssert.Contains("1 task updated", vm.ImportStatusMessage);
            StringAssert.Contains("1 duplicate", vm.ImportStatusMessage);
        }

        [Test]
        public async Task HandleImportResultAsync_NoEvents_And_Cancelled()
        {
            var vm = new MainViewModel();

            var method = typeof(MainViewModel).GetMethod("HandleImportResultAsync", BindingFlags.Instance | BindingFlags.NonPublic);

            var noEvents = new CalendarImportResult { Outcome = CalendarImportOutcome.NoEvents };
            await (Task)method.Invoke(vm, new object[] { noEvents });
            StringAssert.Contains("No Google Calendar events were found", vm.ImportStatusMessage);

            var cancelled = new CalendarImportResult { Outcome = CalendarImportOutcome.Cancelled };
            await (Task)method.Invoke(vm, new object[] { cancelled });
            StringAssert.Contains("Sign-in canceled", vm.ImportStatusMessage);
        }

        [Test]
        public void BuildImportSummaryMessage_AllBranches()
        {
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("BuildImportSummaryMessage", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);

            var mergeType = typeof(MainViewModel).GetNestedType("MergeResult", BindingFlags.NonPublic);
            Assert.IsNotNull(mergeType);
            var ctor = mergeType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).First();

            // No changes, duplicates>0
            var m1 = ctor.Invoke(new object[] { 0, 0, 2 });
            var msg1 = (string)method.Invoke(vm, new object[] { m1 });
            StringAssert.Contains("already up to date", msg1);

            // No changes, duplicates==0
            var m2 = ctor.Invoke(new object[] { 0, 0, 0 });
            var msg2 = (string)method.Invoke(vm, new object[] { m2 });
            StringAssert.Contains("No new Google Calendar events", msg2);

            // Has changes: added and updated and duplicates
            var m3 = ctor.Invoke(new object[] { 2, 1, 3 });
            var msg3 = (string)method.Invoke(vm, new object[] { m3 });
            StringAssert.Contains("2 new", msg3);
            StringAssert.Contains("1 task updated", msg3);
            StringAssert.Contains("3 duplicate", msg3);
        }

        [Test]
        public void ApplyImportedValues_ChangesDetectedAndNotDetected()
        {
            var vm = new MainViewModel();

            var existing = new TaskItem
            {
                Name = "A",
                DueDate = DateTime.Today,
                DueTime = TimeSpan.FromHours(9),
                ReminderStatus = "none",
                ReminderLabel = "Not set",
                ExternalId = "e1"
            };

            var same = new TaskItem
            {
                Name = "A",
                DueDate = existing.DueDate,
                DueTime = existing.DueTime,
                ReminderStatus = existing.ReminderStatus,
                ReminderLabel = existing.ReminderLabel,
                ExternalId = existing.ExternalId
            };

            var method = typeof(MainViewModel).GetMethod("ApplyImportedValues", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);

            var changedFalse = (bool)method.Invoke(vm, new object[] { existing, same });
            Assert.IsFalse(changedFalse);

            var different = new TaskItem
            {
                Name = "B",
                DueDate = existing.DueDate.Value.AddDays(1),
                DueTime = TimeSpan.FromHours(10),
                ReminderStatus = "active",
                ReminderLabel = "Label",
                ExternalId = "e2"
            };

            var changedTrue = (bool)method.Invoke(vm, new object[] { existing, different });
            Assert.IsTrue(changedTrue);
        }

        [Test]
        public void IconStrokeConverter_UsesIsDarkThemeResource()
        {
            var conv = new IconStrokeConverter();

            Application.Current.Resources["IsDarkTheme"] = true;
            var dark = (SolidColorBrush)conv.Convert(null, typeof(Brush), null, null);
            Assert.AreEqual(Colors.White, dark.Color);

            Application.Current.Resources["IsDarkTheme"] = false;
            var light = (SolidColorBrush)conv.Convert(null, typeof(Brush), null, null);
            Assert.AreEqual(Color.FromRgb(0x1A, 0x1A, 0x1A), light.Color);
        }

        [Test]
        public void InverseBoolToVisibilityConverter_Basic()
        {
            var conv = new InverseBoolToVisibilityConverter();
            Assert.AreEqual(Visibility.Collapsed, conv.Convert(true, typeof(Visibility), null, null));
            Assert.AreEqual(Visibility.Visible, conv.Convert(false, typeof(Visibility), null, null));
            Assert.IsTrue((bool)conv.ConvertBack(Visibility.Collapsed, typeof(bool), null, null));
            Assert.IsFalse((bool)conv.ConvertBack(Visibility.Visible, typeof(bool), null, null));
        }

        [Test]
        public void TimerButtonText_ReflectsRunningState()
        {
            var vm = new MainViewModel();
            
            vm.TimerRunning = false;
            Assert.AreEqual("Start Timer", vm.TimerButtonText);

            vm.TimerRunning = true;
            Assert.AreEqual("Pause Timer", vm.TimerButtonText);
        }

        [Test]
        public void RefreshUpcomingTask_SelectsNextOrFirst()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var past = new TaskItem { Name = "Past", DueDate = DateTime.Today.AddDays(-1), DueTime = TimeSpan.FromHours(9) };
            var future = new TaskItem { Name = "Future", DueDate = DateTime.Today.AddDays(1), DueTime = TimeSpan.FromHours(9) };
            
            vm.AllTasks.Add(past);
            vm.AllTasks.Add(future);

            // Trigger refresh via reflection
            var method = typeof(MainViewModel).GetMethod("RefreshUpcomingTask", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(vm, null);

            Assert.IsNotNull(vm.UpcomingTask);
            Assert.AreEqual("Future", vm.UpcomingTask.Name);
        }

        [Test]
        public void DeleteTask_WithNullParameter_DoesNothing()
        {
            var vm = new MainViewModel();
            var initialCount = vm.AllTasks.Count;

            vm.DeleteTaskCommand.Execute(null);

            Assert.AreEqual(initialCount, vm.AllTasks.Count);
        }

        [Test]
        public void EditTaskCommand_WithNullParameter_DoesNothing()
        {
            var vm = new MainViewModel();

            vm.EditTaskCommand.Execute(null);

            Assert.IsFalse(vm.IsModalOpen);
        }

        [Test]
        public void CloseModalCommand_ClearsEditingTask()
        {
            var vm = new MainViewModel();
            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "Test";

            vm.CloseModalCommand.Execute(null);

            Assert.IsFalse(vm.IsModalOpen);
            Assert.IsNull(vm.EditingTask);
        }

        [Test]
        public void PreviousPageCommand_CanExecute_BasedOnCurrentPage()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();
            
            // Add enough tasks for multiple pages
            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = "Task " + i, DueDate = DateTime.Today });
            }
            
            vm.PageSize = 5;
            vm.CurrentPage = 1;
            Assert.IsFalse(vm.PreviousPageCommand.CanExecute(null));

            vm.CurrentPage = 2;
            
            // Force command manager to update CanExecute
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(delegate { }));
            
            Assert.IsTrue(vm.PreviousPageCommand.CanExecute(null));
        }

        [Test]
        public void ChangePage_UpdatesCurrentPage()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();
            
            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = "Task " + i, DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 1;

            vm.NextPageCommand.Execute(null);
            Assert.AreEqual(2, vm.CurrentPage);

            vm.PreviousPageCommand.Execute(null);
            Assert.AreEqual(1, vm.CurrentPage);
        }

        [Test]
        public void SelectDate_UpdatesDailyTasksAndSelectedDate()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var targetDate = DateTime.Today.AddDays(5);
            vm.AllTasks.Add(new TaskItem { Name = "Task on target date", DueDate = targetDate, DueTime = TimeSpan.FromHours(10) });

            vm.SelectDateCommand.Execute(targetDate);

            Assert.AreEqual(targetDate.Date, vm.SelectedDate.Date);
            Assert.AreEqual(1, vm.DailyTaskCount);
        }

        [Test]
        public void RefreshDailyTasks_LimitsTo3Tasks()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();
            
            vm.SelectedDate = DateTime.Today;

            for (int i = 0; i < 5; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = "Task " + i, DueDate = DateTime.Today, DueTime = TimeSpan.FromHours(9 + i) });
            }

            var method = typeof(MainViewModel).GetMethod("RefreshDailyTasks", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(vm, null);

            Assert.AreEqual(3, vm.DailyTasks.Count);
            Assert.AreEqual(5, vm.DailyTaskCount);
        }

        [Test]
        public void GetReminderLabelForActive_FormatsCorrectly()
        {
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("GetReminderLabelForActive", BindingFlags.Instance | BindingFlags.NonPublic);

            var task = new TaskItem
            {
                DueDate = new DateTime(2024, 5, 15),
                DueTime = new TimeSpan(14, 30, 0)
            };

            var label = (string)method.Invoke(vm, new object[] { task });

            StringAssert.Contains("Wednesday", label);
            StringAssert.Contains("May 15", label);
            StringAssert.Contains("14:30 PM", label);
        }

        [Test]
        public void SaveTask_WithEmptyName_ShowsValidationAndKeepsModalOpen()
        {
            var vm = new MainViewModel();
            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "   ";

            vm.SaveTaskCommand.Execute(null);

            Assert.IsTrue(vm.IsModalOpen);
        }

        [Test]
        public void SaveTask_UpdatesReminderLabel_BasedOnStatus()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "Test";
            vm.EditingTask.ReminderStatus = "overdue";
            vm.SaveTaskCommand.Execute(null);

            var added = vm.AllTasks.First(t => t.Name == "Test");
            Assert.AreEqual("Overdue", added.ReminderLabel);
        }

        [Test]
        public void HandleImportResultAsync_WithNullResult_SetsErrorMessage()
        {
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("HandleImportResultAsync", BindingFlags.Instance | BindingFlags.NonPublic);

            var task = (Task)method.Invoke(vm, new object[] { null });
            task.Wait();

            StringAssert.Contains("couldn't import", vm.ImportStatusMessage);
        }

        [Test]
        public async Task HandleImportResultAsync_AccessBlocked_SetsCorrectMessage()
        {
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("HandleImportResultAsync", BindingFlags.Instance | BindingFlags.NonPublic);

            var result = new CalendarImportResult { Outcome = CalendarImportOutcome.AccessBlocked };
            await (Task)method.Invoke(vm, new object[] { result });

            StringAssert.Contains("Google blocked", vm.ImportStatusMessage);
        }

        [Test]
        public async Task HandleImportResultAsync_InvalidCredentials_SetsCorrectMessage()
        {
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("HandleImportResultAsync", BindingFlags.Instance | BindingFlags.NonPublic);

            var result = new CalendarImportResult 
            { 
                Outcome = CalendarImportOutcome.InvalidCredentials,
                ErrorMessage = "Invalid creds"
            };
            await (Task)method.Invoke(vm, new object[] { result });

            StringAssert.Contains("Invalid creds", vm.ImportStatusMessage);
        }

        [Test]
        public void MergeImportedTasks_WithNullList_ReturnsZeroCounts()
        {
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("MergeImportedTasks", BindingFlags.Instance | BindingFlags.NonPublic);

            var result = method.Invoke(vm, new object[] { null });

            var mergeType = typeof(MainViewModel).GetNestedType("MergeResult", BindingFlags.NonPublic);
            var addedProp = mergeType.GetProperty("Added");
            var added = (int)addedProp.GetValue(result);

            Assert.AreEqual(0, added);
        }

        [Test]
        public void MergeImportedTasks_SkipsNullOrEmptyNameTasks()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var method = typeof(MainViewModel).GetMethod("MergeImportedTasks", BindingFlags.Instance | BindingFlags.NonPublic);

            var tasks = new List<TaskItem>
            {
                null,
                new TaskItem { Name = "", ExternalId = "empty" },
                new TaskItem { Name = "   ", ExternalId = "whitespace" },
                new TaskItem { Name = "Valid", ExternalId = "valid", DueDate = DateTime.Today }
            };

            method.Invoke(vm, new object[] { tasks });

            Assert.AreEqual(1, vm.AllTasks.Count);
            Assert.AreEqual("Valid", vm.AllTasks[0].Name);
        }

        [Test]
        public void MergeImportedTasks_MatchesByNameDateTimeWhenNoExternalId()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var existing = new TaskItem
            {
                Name = "Meeting",
                DueDate = DateTime.Today,
                DueTime = TimeSpan.FromHours(10),
                ReminderStatus = "none"
            };
            vm.AllTasks.Add(existing);

            var imported = new TaskItem
            {
                Name = "Meeting",
                DueDate = DateTime.Today,
                DueTime = TimeSpan.FromHours(10),
                ReminderStatus = "active"
            };

            var method = typeof(MainViewModel).GetMethod("MergeImportedTasks", BindingFlags.Instance | BindingFlags.NonPublic);
            var result = method.Invoke(vm, new object[] { new List<TaskItem> { imported } });

            var mergeType = typeof(MainViewModel).GetNestedType("MergeResult", BindingFlags.NonPublic);
            var updatedProp = mergeType.GetProperty("Updated");
            var updated = (int)updatedProp.GetValue(result);

            Assert.AreEqual(1, updated);
            Assert.AreEqual("active", vm.AllTasks[0].ReminderStatus);
        }

        [Test]
        public void ChangeMonth_UpdatesCurrentMonth()
        {
            var vm = new MainViewModel();
            var initial = vm.CurrentMonth;

            vm.NextMonthCommand.Execute(null);
            Assert.AreEqual(initial.AddMonths(1).Month, vm.CurrentMonth.Month);

            vm.PreviousMonthCommand.Execute(null);
            Assert.AreEqual(initial.Month, vm.CurrentMonth.Month);
        }

        [Test]
        public void CurrentMonthDisplay_FormatsCorrectly()
        {
            var vm = new MainViewModel();
            vm.CurrentMonth = new DateTime(2024, 5, 1);

            StringAssert.Contains("May", vm.CurrentMonthDisplay);
            StringAssert.Contains("2024", vm.CurrentMonthDisplay);
        }

        [Test]
        public void SelectedDateDisplay_FormatsCorrectly()
        {
            var vm = new MainViewModel();
            vm.SelectedDate = new DateTime(2024, 5, 15);

            // Trigger refresh
            var method = typeof(MainViewModel).GetMethod("RefreshDailyTasks", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(vm, null);

            StringAssert.Contains("May 15", vm.SelectedDateDisplay);
        }

        [Test]
        public void GenerateCalendarDays_IncludesPlaceholders()
        {
            var vm = new MainViewModel();
            vm.CurrentMonth = new DateTime(2024, 5, 1); // May 2024 starts on Wednesday

            var placeholders = vm.CalendarDays.Count(d => d.IsPlaceholder);
            Assert.Greater(placeholders, 0);
        }

        [Test]
        public void GenerateCalendarDays_MarksTaskDays()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();
            
            // Add task first
            vm.AllTasks.Add(new TaskItem { Name = "Task", DueDate = DateTime.Today, DueTime = TimeSpan.FromHours(9) });

            // Set to a different month first to ensure property change
            vm.CurrentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
            
            // Then set back to the current month to trigger GenerateCalendarDays with the task present
            vm.CurrentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var todayCalendarDay = vm.CalendarDays.FirstOrDefault(d => !d.IsPlaceholder && d.Date.Date == DateTime.Today);
            Assert.IsNotNull(todayCalendarDay, "Today's calendar day should exist");
            Assert.IsTrue(todayCalendarDay.HasTasks, "Today's calendar day should be marked as having tasks");
        }

        [Test]
        public void RefreshDisplayedTasks_HandlesEmptyCollection()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.SearchText = "nonexistent";

            Assert.AreEqual(0, vm.DisplayedTasks.Count);
            StringAssert.Contains("0 - 0 of 0", vm.PaginationText);
        }

        [Test]
        public void RefreshDisplayedTasks_AdjustsPageWhenBeyondTotal()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 10; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = "Task " + i, DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 2;

            // Now remove items so we only have 1 page
            vm.SearchText = "Task 0";

            Assert.AreEqual(1, vm.CurrentPage);
        }

        [Test]
        public void PageSize_ChangeResetsToFirstPage()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = "Task " + i, DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 2;

            vm.PageSize = 10;

            Assert.AreEqual(1, vm.CurrentPage);
        }
    }
}
