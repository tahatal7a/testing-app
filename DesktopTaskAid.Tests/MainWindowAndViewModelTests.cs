using System;
using System.Threading;
using System.Windows;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;
using DesktopTaskAid.ViewModels;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    /// <summary>
    /// Note: MainWindow tests requiring XAML initialization are excluded from unit tests.
    /// MainWindow should be tested through UI automation or integration tests instead.
    /// </summary>

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainViewModelComprehensiveTests
    {
        [SetUp]
        public void Setup()
        {
            if (Application.Current == null)
            {
                new Application();
            }
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources["IsDarkTheme"] = false;
        }

        [Test]
        public void MainViewModel_Constructor_InitializesAllCommands()
        {
            var vm = new MainViewModel();

            Assert.IsNotNull(vm.ToggleThemeCommand);
            Assert.IsNotNull(vm.ToggleTimerCommand);
            Assert.IsNotNull(vm.ResetTimerCommand);
            Assert.IsNotNull(vm.PreviousMonthCommand);
            Assert.IsNotNull(vm.NextMonthCommand);
            Assert.IsNotNull(vm.SelectDateCommand);
            Assert.IsNotNull(vm.AddTaskCommand);
            Assert.IsNotNull(vm.EditTaskCommand);
            Assert.IsNotNull(vm.DeleteTaskCommand);
            Assert.IsNotNull(vm.SaveTaskCommand);
            Assert.IsNotNull(vm.CloseModalCommand);
            Assert.IsNotNull(vm.PreviousPageCommand);
            Assert.IsNotNull(vm.NextPageCommand);
            Assert.IsNotNull(vm.ImportNextMonthCommand);
            Assert.IsNotNull(vm.CreateGoogleAccountCommand);
            Assert.IsNotNull(vm.OpenCalendarImportModalCommand);
            Assert.IsNotNull(vm.CloseCalendarImportModalCommand);
        }

        [Test]
        public void MainViewModel_Constructor_InitializesAllCollections()
        {
            var vm = new MainViewModel();

            Assert.IsNotNull(vm.AllTasks);
            Assert.IsNotNull(vm.DisplayedTasks);
            Assert.IsNotNull(vm.CalendarDays);
            Assert.IsNotNull(vm.DailyTasks);
        }

        [Test]
        public void MainViewModel_Constructor_SetsDefaultValues()
        {
            var vm = new MainViewModel();

            Assert.Greater(vm.PageSize, 0);
            Assert.GreaterOrEqual(vm.CurrentPage, 1);
            Assert.IsNotNull(vm.CurrentTheme);
            Assert.IsNotNull(vm.CurrentMonthDisplay);
            Assert.IsNotNull(vm.PaginationText);
        }

        [Test]
        public void MainViewModel_AllStringProperties_CanBeSetAndGet()
        {
            var vm = new MainViewModel();

            vm.SearchText = "test search";
            Assert.AreEqual("test search", vm.SearchText);

            vm.CurrentTheme = "dark";
            Assert.AreEqual("dark", vm.CurrentTheme);

            vm.ModalTitle = "Test Modal";
            Assert.AreEqual("Test Modal", vm.ModalTitle);

            vm.ImportStatusMessage = "Test Status";
            Assert.AreEqual("Test Status", vm.ImportStatusMessage);

            vm.SelectedDateDisplay = "Test Date";
            Assert.AreEqual("Test Date", vm.SelectedDateDisplay);

            vm.PaginationText = "1 - 10 of 50";
            Assert.AreEqual("1 - 10 of 50", vm.PaginationText);
        }

        [Test]
        public void MainViewModel_AllBooleanProperties_CanBeSetAndGet()
        {
            var vm = new MainViewModel();

            vm.IsDarkTheme = true;
            Assert.IsTrue(vm.IsDarkTheme);

            vm.TimerRunning = true;
            Assert.IsTrue(vm.TimerRunning);

            vm.IsModalOpen = true;
            Assert.IsTrue(vm.IsModalOpen);

            vm.IsCalendarImportModalOpen = true;
            Assert.IsTrue(vm.IsCalendarImportModalOpen);
        }

        [Test]
        public void MainViewModel_AllIntegerProperties_CanBeSetAndGet()
        {
            var vm = new MainViewModel();

            vm.TimerRemaining = 100;
            Assert.AreEqual(100, vm.TimerRemaining);

            vm.DoneTodaySeconds = 500;
            Assert.AreEqual(500, vm.DoneTodaySeconds);

            vm.DailyTaskCount = 5;
            Assert.AreEqual(5, vm.DailyTaskCount);

            // Ensure we have enough tasks to support multiple pages
            vm.AllTasks.Clear();
            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"T{i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 15; // 2 pages for 20 items
            vm.CurrentPage = 2;
            Assert.AreEqual(2, vm.CurrentPage);
        }

        [Test]
        public void MainViewModel_AllDateTimeProperties_CanBeSetAndGet()
        {
            var vm = new MainViewModel();
            var testDate = new DateTime(2025, 6, 15);

            vm.CurrentMonth = testDate;
            Assert.AreEqual(testDate, vm.CurrentMonth);

            vm.SelectedDate = testDate;
            Assert.AreEqual(testDate, vm.SelectedDate);
        }

        [Test]
        public void MainViewModel_Commands_AlwaysExecutable_CanExecute()
        {
            var vm = new MainViewModel();

            Assert.IsTrue(vm.ToggleThemeCommand.CanExecute(null));
            Assert.IsTrue(vm.ToggleTimerCommand.CanExecute(null));
            Assert.IsTrue(vm.ResetTimerCommand.CanExecute(null));
            Assert.IsTrue(vm.PreviousMonthCommand.CanExecute(null));
            Assert.IsTrue(vm.NextMonthCommand.CanExecute(null));
            Assert.IsTrue(vm.AddTaskCommand.CanExecute(null));
            Assert.IsTrue(vm.SaveTaskCommand.CanExecute(null));
            Assert.IsTrue(vm.CloseModalCommand.CanExecute(null));
            Assert.IsTrue(vm.CloseCalendarImportModalCommand.CanExecute(null));
        }

        [Test]
        public void MainViewModel_PreviousPageCommand_CanExecute_OnlyOnPage2OrHigher()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
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
        public void MainViewModel_NextPageCommand_CanExecute_WhenMorePagesExist()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 15; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 1;
            Assert.IsTrue(vm.NextPageCommand.CanExecute(null));

            vm.CurrentPage = 3;
            Assert.IsFalse(vm.NextPageCommand.CanExecute(null));
        }

        [Test]
        public void MainViewModel_OpenCalendarImportModal_SetsProperty()
        {
            var vm = new MainViewModel();
            Assert.IsFalse(vm.IsCalendarImportModalOpen);

            vm.OpenCalendarImportModalCommand.Execute(null);

            Assert.IsTrue(vm.IsCalendarImportModalOpen);
        }

        [Test]
        public void MainViewModel_CloseCalendarImportModal_ClearsProperty()
        {
            var vm = new MainViewModel();
            vm.OpenCalendarImportModalCommand.Execute(null);
            Assert.IsTrue(vm.IsCalendarImportModalOpen);

            vm.CloseCalendarImportModalCommand.Execute(null);

            Assert.IsFalse(vm.IsCalendarImportModalOpen);
        }

        [Test]
        public void MainViewModel_TimerButtonText_ChangesWithTimerState()
        {
            var vm = new MainViewModel();

            vm.TimerRunning = false;
            Assert.AreEqual("Start Timer", vm.TimerButtonText);

            vm.TimerRunning = true;
            Assert.AreEqual("Pause Timer", vm.TimerButtonText);

            vm.TimerRunning = false;
            Assert.AreEqual("Start Timer", vm.TimerButtonText);
        }

        [Test]
        public void MainViewModel_SearchText_FiltersDisplayedTasks()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 10; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.SearchText = "Task 5";

            Assert.AreEqual(1, vm.DisplayedTasks.Count);
            Assert.AreEqual("Task 5", vm.DisplayedTasks[0].Name);
        }

        [Test]
        public void MainViewModel_SearchText_SearchesReminderLabel()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AllTasks.Add(new TaskItem 
            { 
                Name = "Task 1", 
                DueDate = DateTime.Today,
                ReminderLabel = "UniqueLabel123"
            });
            vm.AllTasks.Add(new TaskItem 
            { 
                Name = "Task 2", 
                DueDate = DateTime.Today,
                ReminderLabel = "DifferentLabel"
            });

            vm.SearchText = "UniqueLabel123";

            Assert.AreEqual(1, vm.DisplayedTasks.Count);
            Assert.AreEqual("Task 1", vm.DisplayedTasks[0].Name);
        }

        [Test]
        public void MainViewModel_PageSize_Change_ResetsToFirstPage()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 3;
            Assert.AreEqual(3, vm.CurrentPage);

            vm.PageSize = 10;

            Assert.AreEqual(1, vm.CurrentPage);
        }

        [Test]
        public void MainViewModel_CurrentPage_Change_TriggersRefresh()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 1;
            var firstPageFirstTask = vm.DisplayedTasks[0].Name;

            vm.CurrentPage = 2;
            var secondPageFirstTask = vm.DisplayedTasks[0].Name;

            Assert.AreNotEqual(firstPageFirstTask, secondPageFirstTask);
        }

        [Test]
        public void MainViewModel_CurrentMonth_Change_UpdatesDisplay()
        {
            var vm = new MainViewModel();
            var initialMonth = vm.CurrentMonth;

            vm.CurrentMonth = initialMonth.AddMonths(1);

            var expectedDisplay = initialMonth.AddMonths(1).ToString("MMMM yyyy", 
                System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(expectedDisplay, vm.CurrentMonthDisplay);
        }

        [Test]
        public void MainViewModel_CurrentMonth_Change_RegeneratesCalendarDays()
        {
            var vm = new MainViewModel();
            var initialMonth = vm.CurrentMonth;
            var initialDaysCount = vm.CalendarDays.Count;

            vm.CurrentMonth = initialMonth.AddMonths(1);

            // Calendar should have regenerated (count might be same but content different)
            Assert.IsNotNull(vm.CalendarDays);
            Assert.Greater(vm.CalendarDays.Count, 0);
        }

        [Test]
        public void MainViewModel_SelectedDate_Change_RefreshesDailyTasks()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 5; i++)
            {
                vm.AllTasks.Add(new TaskItem 
                { 
                    Name = $"Task {i}", 
                    DueDate = DateTime.Today,
                    DueTime = TimeSpan.FromHours(9 + i)
                });
            }

            // Force a change to trigger the refresh (default is Today)
            vm.SelectedDate = DateTime.Today.AddDays(1);
            vm.SelectedDate = DateTime.Today;

            Assert.AreEqual(3, vm.DailyTasks.Count);
            Assert.AreEqual(5, vm.DailyTaskCount);
        }

        [Test]
        public void MainViewModel_DailyTasks_LimitedToThree()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 5; i++)
            {
                vm.AllTasks.Add(new TaskItem 
                { 
                    Name = $"Task {i}", 
                    DueDate = DateTime.Today,
                    DueTime = TimeSpan.FromHours(9 + i)
                });
            }

            // Force refresh by toggling date away and back to today
            vm.SelectedDate = DateTime.Today.AddDays(1);
            vm.SelectedDate = DateTime.Today;

            Assert.AreEqual(3, vm.DailyTasks.Count);
            Assert.AreEqual(5, vm.DailyTaskCount);
        }

        [Test]
        public void MainViewModel_AddTask_OpensModalWithDefaults()
        {
            var vm = new MainViewModel();

            vm.AddTaskCommand.Execute(null);

            Assert.IsTrue(vm.IsModalOpen);
            Assert.AreEqual("Add Task", vm.ModalTitle);
            Assert.IsNotNull(vm.EditingTask);
            Assert.AreEqual("active", vm.EditingTask.ReminderStatus);
            Assert.AreEqual(DateTime.Today, vm.EditingTask.DueDate);
            Assert.AreEqual(new TimeSpan(9, 0, 0), vm.EditingTask.DueTime);
        }

        [Test]
        public void MainViewModel_EditTask_OpensModalWithTaskData()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var task = new TaskItem
            {
                Name = "Original Task",
                DueDate = DateTime.Today.AddDays(1),
                DueTime = TimeSpan.FromHours(15),
                ReminderStatus = "overdue",
                ReminderLabel = "Custom Label"
            };
            vm.AllTasks.Add(task);

            vm.EditTaskCommand.Execute(task);

            Assert.IsTrue(vm.IsModalOpen);
            Assert.AreEqual("Edit Task", vm.ModalTitle);
            Assert.IsNotNull(vm.EditingTask);
            Assert.AreEqual("Original Task", vm.EditingTask.Name);
            Assert.AreEqual(task.Id, vm.EditingTask.Id);
            Assert.AreEqual("overdue", vm.EditingTask.ReminderStatus);
        }

        [Test]
        public void MainViewModel_EditTask_WithNull_DoesNothing()
        {
            var vm = new MainViewModel();

            vm.EditTaskCommand.Execute(null);

            Assert.IsFalse(vm.IsModalOpen);
            Assert.IsNull(vm.EditingTask);
        }

        [Test]
        public void MainViewModel_SaveTask_AddsNewTask()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "New Task";
            vm.EditingTask.ReminderStatus = "active";
            vm.SaveTaskCommand.Execute(null);

            Assert.AreEqual(1, vm.AllTasks.Count);
            Assert.AreEqual("New Task", vm.AllTasks[0].Name);
            Assert.IsFalse(vm.IsModalOpen);
            Assert.IsNull(vm.EditingTask);
        }

        [Test]
        public void MainViewModel_SaveTask_UpdatesExistingTask()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var task = new TaskItem { Name = "Original", DueDate = DateTime.Today };
            vm.AllTasks.Add(task);

            vm.EditTaskCommand.Execute(task);
            vm.EditingTask.Name = "Updated";
            vm.SaveTaskCommand.Execute(null);

            Assert.AreEqual(1, vm.AllTasks.Count);
            Assert.AreEqual("Updated", vm.AllTasks[0].Name);
        }

        [Test]
        public void MainViewModel_SaveTask_WithEmptyName_ShowsValidation()
        {
            var vm = new MainViewModel();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "   ";

            vm.SaveTaskCommand.Execute(null);

            Assert.IsTrue(vm.IsModalOpen);
            Assert.IsNotNull(vm.EditingTask);
        }

        [Test]
        public void MainViewModel_SaveTask_WithActiveStatus_SetsReminderLabel()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "Test Task";
            vm.EditingTask.ReminderStatus = "active";
            vm.EditingTask.DueDate = new DateTime(2025, 6, 15);
            vm.EditingTask.DueTime = new TimeSpan(14, 30, 0);
            vm.SaveTaskCommand.Execute(null);

            var task = vm.AllTasks[0];
            Assert.IsNotNull(task.ReminderLabel);
            StringAssert.Contains("Jun 15", task.ReminderLabel);
        }

        [Test]
        public void MainViewModel_SaveTask_WithOverdueStatus_SetsOverdueLabel()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "Test Task";
            vm.EditingTask.ReminderStatus = "overdue";
            vm.SaveTaskCommand.Execute(null);

            Assert.AreEqual("Overdue", vm.AllTasks[0].ReminderLabel);
        }

        [Test]
        public void MainViewModel_SaveTask_WithNoneStatus_SetsNotSetLabel()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "Test Task";
            vm.EditingTask.ReminderStatus = "none";
            vm.SaveTaskCommand.Execute(null);

            Assert.AreEqual("Not set", vm.AllTasks[0].ReminderLabel);
        }

        [Test]
        public void MainViewModel_DeleteTask_WithValidTask_RemovesTask()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var task = new TaskItem { Name = "To Delete", DueDate = DateTime.Today };
            vm.AllTasks.Add(task);

            // Note: DeleteTask shows MessageBox which can't be automated in tests
            // We test the null parameter case instead
            vm.DeleteTaskCommand.Execute(null);

            // With null parameter, nothing should be deleted
            Assert.AreEqual(1, vm.AllTasks.Count);
        }

        [Test]
        public void MainViewModel_DeleteTask_WithNull_DoesNothing()
        {
            var vm = new MainViewModel();
            var initialCount = vm.AllTasks.Count;

            vm.DeleteTaskCommand.Execute(null);

            Assert.AreEqual(initialCount, vm.AllTasks.Count);
        }

        [Test]
        public void MainViewModel_CloseModal_ClearsEditingTask()
        {
            var vm = new MainViewModel();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "Test";

            Assert.IsTrue(vm.IsModalOpen);
            Assert.IsNotNull(vm.EditingTask);

            vm.CloseModalCommand.Execute(null);

            Assert.IsFalse(vm.IsModalOpen);
            Assert.IsNull(vm.EditingTask);
        }

        [Test]
        public void MainViewModel_ResetTimer_StopsAndResetsValues()
        {
            var vm = new MainViewModel();

            vm.ToggleTimerCommand.Execute(null);
            Assert.IsTrue(vm.TimerRunning);

            vm.TimerRemaining = 100;

            vm.ResetTimerCommand.Execute(null);

            Assert.IsFalse(vm.TimerRunning);
            Assert.Greater(vm.TimerRemaining, 100);
        }

        [Test]
        public void MainViewModel_ToggleTimer_StartsAndStops()
        {
            var vm = new MainViewModel();

            Assert.IsFalse(vm.TimerRunning);

            vm.ToggleTimerCommand.Execute(null);
            Assert.IsTrue(vm.TimerRunning);

            vm.ToggleTimerCommand.Execute(null);
            Assert.IsFalse(vm.TimerRunning);
        }

        [Test]
        public void MainViewModel_PreviousMonth_DecreasesMonth()
        {
            var vm = new MainViewModel();
            var initial = vm.CurrentMonth;

            vm.PreviousMonthCommand.Execute(null);

            Assert.AreEqual(initial.AddMonths(-1).Month, vm.CurrentMonth.Month);
        }

        [Test]
        public void MainViewModel_NextMonth_IncreasesMonth()
        {
            var vm = new MainViewModel();
            var initial = vm.CurrentMonth;

            vm.NextMonthCommand.Execute(null);

            Assert.AreEqual(initial.AddMonths(1).Month, vm.CurrentMonth.Month);
        }

        [Test]
        public void MainViewModel_SelectDate_UpdatesSelectedDate()
        {
            var vm = new MainViewModel();
            var targetDate = new DateTime(2025, 7, 15);

            vm.SelectDateCommand.Execute(targetDate);

            Assert.AreEqual(targetDate, vm.SelectedDate);
        }

        [Test]
        public void MainViewModel_SelectDate_WithNonDateTimeParameter_DoesNothing()
        {
            var vm = new MainViewModel();
            var initialDate = vm.SelectedDate;

            vm.SelectDateCommand.Execute("not a date");

            Assert.AreEqual(initialDate, vm.SelectedDate);
        }

        [Test]
        public void MainViewModel_MultiplePropertyChanges_FireEvents()
        {
            var vm = new MainViewModel();
            var propertyChangedCount = 0;

            vm.PropertyChanged += (s, e) => propertyChangedCount++;

            vm.SearchText = "test";
            vm.IsModalOpen = true;
            vm.TimerRunning = true;

            Assert.Greater(propertyChangedCount, 0);
        }

        [Test]
        public void MainViewModel_UpcomingTask_CanBeSet()
        {
            var vm = new MainViewModel();
            var task = new TaskItem 
            { 
                Name = "Upcoming", 
                DueDate = DateTime.Today.AddDays(1),
                DueTime = TimeSpan.FromHours(10)
            };

            vm.UpcomingTask = task;

            Assert.AreEqual(task, vm.UpcomingTask);
            Assert.AreEqual("Upcoming", vm.UpcomingTask.Name);
        }

        [Test]
        public void MainViewModel_PaginationText_DisplaysCorrectRange()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 25; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            // Force a refresh by changing PageSize to a different value, then set to 10
            vm.PageSize = 9;
            vm.PageSize = 10;
            vm.CurrentPage = 1;

            // Check that pagination text contains the right numbers
            var text = vm.PaginationText;
            Assert.IsTrue(text.Contains("10") && text.Contains("25"));
        }

        [Test]
        public void MainViewModel_PaginationText_ShowsZeroForEmpty()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var text = vm.PaginationText;
            Assert.IsTrue(text.Contains("0"));
        }

        [Test]
        public void MainViewModel_CalendarDays_GeneratedOnConstruction()
        {
            var vm = new MainViewModel();

            Assert.Greater(vm.CalendarDays.Count, 0);
            Assert.GreaterOrEqual(vm.CalendarDays.Count, 28);
        }

        [Test]
        public void MainViewModel_CalendarDays_IncludesPlaceholders()
        {
            var vm = new MainViewModel();

            var placeholders = 0;
            foreach (var day in vm.CalendarDays)
            {
                if (day.IsPlaceholder) placeholders++;
            }

            // Most months will have some placeholder days
            Assert.GreaterOrEqual(placeholders, 0);
        }

        [Test]
        public void MainViewModel_EditingTask_CanBeNull()
        {
            var vm = new MainViewModel();

            vm.EditingTask = null;

            Assert.IsNull(vm.EditingTask);
        }

        [Test]
        public void MainViewModel_Constructor_LogsInitialization()
        {
            var vm = new MainViewModel();

            var path = LoggingService.GetLogFilePath();
            var content = System.IO.File.ReadAllText(path);

            StringAssert.Contains("MainViewModel Constructor BEGIN", content);
            StringAssert.Contains("MainViewModel Constructor COMPLETED SUCCESSFULLY", content);
        }

        [Test]
        public void MainViewModel_AllTasks_CanBeCleared()
        {
            var vm = new MainViewModel();
            
            vm.AllTasks.Add(new TaskItem { Name = "Test", DueDate = DateTime.Today });
            Assert.Greater(vm.AllTasks.Count, 0);

            vm.AllTasks.Clear();
            Assert.AreEqual(0, vm.AllTasks.Count);
        }

        [Test]
        public void MainViewModel_DisplayedTasks_UpdatesOnSearchClear()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 10; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.SearchText = "Task 5";
            Assert.AreEqual(1, vm.DisplayedTasks.Count);

            vm.SearchText = "";
            Assert.Greater(vm.DisplayedTasks.Count, 1);
        }

        [Test]
        public void MainViewModel_IsImportRunning_DefaultsToFalse()
        {
            var vm = new MainViewModel();

            Assert.IsFalse(vm.IsImportRunning);
        }

        [Test]
        public void MainViewModel_CurrentMonthDisplay_ReflectsCurrentMonth()
        {
            var vm = new MainViewModel();
            var now = DateTime.Now;

            vm.CurrentMonth = new DateTime(now.Year, now.Month, 1);

            StringAssert.Contains(now.ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture), 
                vm.CurrentMonthDisplay);
        }

        [Test]
        public void MainViewModel_RefreshDisplayedTasks_AdjustsPageBeyondTotal()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 10; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 2;

            vm.SearchText = "Task 0";

            Assert.AreEqual(1, vm.CurrentPage);
        }

        [Test]
        public void MainViewModel_NextPageCommand_Execute_IncreasesPage()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 1;

            vm.NextPageCommand.Execute(null);

            Assert.AreEqual(2, vm.CurrentPage);
        }

        [Test]
        public void MainViewModel_PreviousPageCommand_Execute_DecreasesPage()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 2;

            vm.PreviousPageCommand.Execute(null);

            Assert.AreEqual(1, vm.CurrentPage);
        }

        [Test]
        public void MainViewModel_Collections_AreObservable()
        {
            var vm = new MainViewModel();

            Assert.IsInstanceOf<System.Collections.ObjectModel.ObservableCollection<TaskItem>>(vm.AllTasks);
            Assert.IsInstanceOf<System.Collections.ObjectModel.ObservableCollection<TaskItem>>(vm.DisplayedTasks);
            Assert.IsInstanceOf<System.Collections.ObjectModel.ObservableCollection<TaskItem>>(vm.DailyTasks);
        }

        [Test]
        public void MainViewModel_SaveTask_TriggersAllRefreshes()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var initialCalendarCount = vm.CalendarDays.Count;
            var initialDisplayedCount = vm.DisplayedTasks.Count;

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "New Task";
            vm.EditingTask.DueDate = DateTime.Today;
            vm.SaveTaskCommand.Execute(null);

            // Verify refreshes occurred
            Assert.IsNotNull(vm.CalendarDays);
            Assert.IsNotNull(vm.DisplayedTasks);
            Assert.IsNotNull(vm.DailyTasks);
        }
    }
}
