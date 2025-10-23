using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DesktopTaskAid.Helpers;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;

namespace DesktopTaskAid.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly StorageService _storageService;
        private readonly DispatcherTimer _timerTick;
        private AppState _state;

        // Event for theme changes
        public event Action ThemeChanged;

        #region Properties

        private ObservableCollection<TaskItem> _allTasks;
        public ObservableCollection<TaskItem> AllTasks
        {
            get => _allTasks;
            set => SetProperty(ref _allTasks, value);
        }

        private ObservableCollection<TaskItem> _displayedTasks;
        public ObservableCollection<TaskItem> DisplayedTasks
        {
            get => _displayedTasks;
            set => SetProperty(ref _displayedTasks, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RefreshDisplayedTasks();
                }
            }
        }

        private string _currentTheme;
        public string CurrentTheme
        {
            get => _currentTheme;
            set => SetProperty(ref _currentTheme, value);
        }

        private bool _isDarkTheme;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set => SetProperty(ref _isDarkTheme, value);
        }

        private TaskItem _upcomingTask;
        public TaskItem UpcomingTask
        {
            get => _upcomingTask;
            set => SetProperty(ref _upcomingTask, value);
        }

        private int _timerRemaining;
        public int TimerRemaining
        {
            get => _timerRemaining;
            set => SetProperty(ref _timerRemaining, value);
        }

        private bool _timerRunning;
        public bool TimerRunning
        {
            get => _timerRunning;
            set
            {
                if (SetProperty(ref _timerRunning, value))
                {
                    OnPropertyChanged(nameof(TimerButtonText));
                }
            }
        }

        public string TimerButtonText => TimerRunning ? "Pause Timer" : "Start Timer";

        private int _doneTodaySeconds;
        public int DoneTodaySeconds
        {
            get => _doneTodaySeconds;
            set => SetProperty(ref _doneTodaySeconds, value);
        }

        private DateTime _currentMonth;
        public DateTime CurrentMonth
        {
            get => _currentMonth;
            set
            {
                if (SetProperty(ref _currentMonth, value))
                {
                    OnPropertyChanged(nameof(CurrentMonthDisplay));
                    GenerateCalendarDays();
                }
            }
        }

        public string CurrentMonthDisplay => CurrentMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        private DateTime _selectedDate;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    RefreshDailyTasks();
                }
            }
        }

        private ObservableCollection<CalendarDay> _calendarDays;
        public ObservableCollection<CalendarDay> CalendarDays
        {
            get => _calendarDays;
            set => SetProperty(ref _calendarDays, value);
        }

        private ObservableCollection<TaskItem> _dailyTasks;
        public ObservableCollection<TaskItem> DailyTasks
        {
            get => _dailyTasks;
            set => SetProperty(ref _dailyTasks, value);
        }

        private string _selectedDateDisplay;
        public string SelectedDateDisplay
        {
            get => _selectedDateDisplay;
            set => SetProperty(ref _selectedDateDisplay, value);
        }

        private int _dailyTaskCount;
        public int DailyTaskCount
        {
            get => _dailyTaskCount;
            set => SetProperty(ref _dailyTaskCount, value);
        }

        private int _currentPage;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    RefreshDisplayedTasks();
                }
            }
        }

        private int _pageSize;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                    RefreshDisplayedTasks();
                }
            }
        }

        private string _paginationText;
        public string PaginationText
        {
            get => _paginationText;
            set => SetProperty(ref _paginationText, value);
        }

        private TaskItem _editingTask;
        public TaskItem EditingTask
        {
            get => _editingTask;
            set => SetProperty(ref _editingTask, value);
        }

        private bool _isModalOpen;
        public bool IsModalOpen
        {
            get => _isModalOpen;
            set => SetProperty(ref _isModalOpen, value);
        }

        private string _modalTitle;
        public string ModalTitle
        {
            get => _modalTitle;
            set => SetProperty(ref _modalTitle, value);
        }

        private bool _isSyncModalOpen;
        public bool IsSyncModalOpen
        {
            get => _isSyncModalOpen;
            set => SetProperty(ref _isSyncModalOpen, value);
        }

        private bool _isCalendarUrlModalOpen;
        public bool IsCalendarUrlModalOpen
        {
            get => _isCalendarUrlModalOpen;
            set => SetProperty(ref _isCalendarUrlModalOpen, value);
        }

        private string _syncUrl;
        public string SyncUrl
        {
            get => _syncUrl;
            set => SetProperty(ref _syncUrl, value);
        }

        #endregion

        #region Commands

        public ICommand ToggleThemeCommand { get; }
        public ICommand ToggleTimerCommand { get; }
        public ICommand ResetTimerCommand { get; }
        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand SelectDateCommand { get; }
        public ICommand AddTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand SaveTaskCommand { get; }
        public ICommand CloseModalCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand OpenSyncModalCommand { get; }
        public ICommand CloseSyncModalCommand { get; }
        public ICommand OpenCalendarUrlModalCommand { get; }
        public ICommand CloseCalendarUrlModalCommand { get; }
        public ICommand ImportSyncUrlCommand { get; }

        #endregion

        public MainViewModel()
        {
            LoggingService.Log("=== MainViewModel Constructor BEGIN ===");
            
            try
            {
                LoggingService.Log("Creating StorageService");
                _storageService = new StorageService();
                
                LoggingService.Log("Loading application state");
                _state = _storageService.LoadState();
                LoggingService.Log($"State loaded - Tasks count: {_state?.Tasks?.Count ?? 0}");

                // Initialize collections
                LoggingService.Log("Initializing collections");
                AllTasks = new ObservableCollection<TaskItem>(_state.Tasks);
                DisplayedTasks = new ObservableCollection<TaskItem>();
                CalendarDays = new ObservableCollection<CalendarDay>();
                DailyTasks = new ObservableCollection<TaskItem>();
                LoggingService.Log($"Collections initialized - AllTasks: {AllTasks.Count}");

                // Initialize properties
                LoggingService.Log("Initializing properties");
                CurrentTheme = _state.Settings.Theme;
                IsDarkTheme = CurrentTheme == "dark";
                TimerRemaining = _state.Timer.RemainingSeconds;
                TimerRunning = false; // Always start stopped
                DoneTodaySeconds = _state.Timer.DoneTodaySeconds;
                CurrentMonth = _state.Calendar.CurrentMonth;
                SelectedDate = _state.Calendar.SelectedDate;
                CurrentPage = _state.CurrentPage;
                PageSize = _state.PageSize;
                SearchText = string.Empty;
                LoggingService.Log($"Properties initialized - Theme: {CurrentTheme}, CurrentMonth: {CurrentMonth}");

                // Initialize commands
                LoggingService.Log("Initializing commands");
                ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
                ToggleTimerCommand = new RelayCommand(_ => ToggleTimer());
                ResetTimerCommand = new RelayCommand(_ => ResetTimer());
                PreviousMonthCommand = new RelayCommand(_ => ChangeMonth(-1));
                NextMonthCommand = new RelayCommand(_ => ChangeMonth(1));
                SelectDateCommand = new RelayCommand(param => SelectDate(param));
                AddTaskCommand = new RelayCommand(_ => OpenAddTaskModal());
                EditTaskCommand = new RelayCommand(param => OpenEditTaskModal(param as TaskItem));
                DeleteTaskCommand = new RelayCommand(param => DeleteTask(param as TaskItem));
                SaveTaskCommand = new RelayCommand(_ => SaveTask());
                CloseModalCommand = new RelayCommand(_ => CloseModal());
                PreviousPageCommand = new RelayCommand(_ => ChangePage(-1), _ => CurrentPage > 1);
                NextPageCommand = new RelayCommand(_ => ChangePage(1), _ => CanGoNextPage());
                OpenSyncModalCommand = new RelayCommand(_ => OpenSyncModal());
                CloseSyncModalCommand = new RelayCommand(_ => CloseSyncModal());
                OpenCalendarUrlModalCommand = new RelayCommand(_ => OpenCalendarUrlModal());
                CloseCalendarUrlModalCommand = new RelayCommand(_ => CloseCalendarUrlModal());
                ImportSyncUrlCommand = new RelayCommand(_ => ImportSyncUrl());
                LoggingService.Log("Commands initialized");

                // Setup timer
                LoggingService.Log("Setting up timer");
                _timerTick = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _timerTick.Tick += TimerTick;
                LoggingService.Log("Timer setup complete");

                // Initial render
                LoggingService.Log("Starting initial render");
                RefreshUpcomingTask();
                LoggingService.Log("RefreshUpcomingTask completed");
                
                GenerateCalendarDays();
                LoggingService.Log($"GenerateCalendarDays completed - CalendarDays count: {CalendarDays.Count}");
                
                RefreshDailyTasks();
                LoggingService.Log($"RefreshDailyTasks completed - DailyTasks count: {DailyTasks.Count}");
                
                RefreshDisplayedTasks();
                LoggingService.Log($"RefreshDisplayedTasks completed - DisplayedTasks count: {DisplayedTasks.Count}");
                
                LoggingService.Log("=== MainViewModel Constructor COMPLETED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("ERROR in MainViewModel constructor", ex);
                throw; // Re-throw to trigger global exception handler
            }
        }

        private void ToggleTheme()
        {
            IsDarkTheme = !IsDarkTheme;
            CurrentTheme = IsDarkTheme ? "dark" : "light";
            _state.Settings.Theme = CurrentTheme;
            SaveState();

            // Apply theme to application
            Application.Current.Resources.MergedDictionaries.Clear();
            var themeDict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/Themes/{CurrentTheme}Theme.xaml")
            };
            Application.Current.Resources.MergedDictionaries.Add(themeDict);
            
            // Notify listeners (e.g., MainWindow) that theme has changed
            ThemeChanged?.Invoke();
        }

        private void ToggleTimer()
        {
            TimerRunning = !TimerRunning;

            if (TimerRunning)
            {
                _timerTick.Start();
            }
            else
            {
                _timerTick.Stop();
            }

            _state.Timer.IsRunning = TimerRunning;
            SaveState();
        }

        private void ResetTimer()
        {
            TimerRunning = false;
            _timerTick.Stop();
            TimerRemaining = _state.Timer.DurationSeconds;
            _state.Timer.RemainingSeconds = TimerRemaining;
            _state.Timer.IsRunning = false;
            SaveState();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (TimerRemaining > 0)
            {
                TimerRemaining--;
                _state.Timer.RemainingSeconds = TimerRemaining;
            }
            else
            {
                // Timer completed
                TimerRunning = false;
                _timerTick.Stop();
                DoneTodaySeconds += _state.Timer.DurationSeconds;
                _state.Timer.DoneTodaySeconds = DoneTodaySeconds;
                TimerRemaining = _state.Timer.DurationSeconds;
                _state.Timer.RemainingSeconds = TimerRemaining;
                _state.Timer.IsRunning = false;
                
                MessageBox.Show("Great job! Timer completed.", "Timer", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            SaveState();
        }

        private void ChangeMonth(int offset)
        {
            CurrentMonth = CurrentMonth.AddMonths(offset);
            _state.Calendar.CurrentMonth = CurrentMonth;
            SaveState();
        }

        private void SelectDate(object param)
        {
            if (param is DateTime date)
            {
                SelectedDate = date;
                _state.Calendar.SelectedDate = SelectedDate;
                SaveState();
            }
        }

        private void GenerateCalendarDays()
        {
            CalendarDays.Clear();

            var firstDay = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            var startWeekday = (int)firstDay.DayOfWeek;
            var daysInMonth = DateTime.DaysInMonth(CurrentMonth.Year, CurrentMonth.Month);

            // Add placeholder days
            for (int i = 0; i < startWeekday; i++)
            {
                CalendarDays.Add(new CalendarDay { IsPlaceholder = true });
            }

            // Add actual days
            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(CurrentMonth.Year, CurrentMonth.Month, day);
                var hasTasks = AllTasks.Any(t => t.DueDate?.Date == date.Date);
                var isToday = date.Date == DateTime.Today;
                var isSelected = date.Date == SelectedDate.Date;

                CalendarDays.Add(new CalendarDay
                {
                    Date = date,
                    Day = day,
                    IsPlaceholder = false,
                    HasTasks = hasTasks,
                    IsToday = isToday,
                    IsSelected = isSelected
                });
            }
        }

        private void RefreshDailyTasks()
        {
            DailyTasks.Clear();

            var tasksForDay = AllTasks
                .Where(t => t.DueDate?.Date == SelectedDate.Date)
                .OrderBy(t => t.DueTime ?? TimeSpan.MaxValue)
                .Take(3)
                .ToList();

            foreach (var task in tasksForDay)
            {
                DailyTasks.Add(task);
            }

            DailyTaskCount = AllTasks.Count(t => t.DueDate?.Date == SelectedDate.Date);
            SelectedDateDisplay = SelectedDate.ToString("dddd, MMM dd", CultureInfo.InvariantCulture);
        }

        private void RefreshUpcomingTask()
        {
            var now = DateTime.Now;
            var sortedTasks = AllTasks
                .Where(t => t.DueDate.HasValue)
                .OrderBy(t => t.GetFullDueDateTime())
                .ToList();

            UpcomingTask = sortedTasks.FirstOrDefault(t => t.GetFullDueDateTime() >= now)
                ?? sortedTasks.FirstOrDefault();
        }

        private void RefreshDisplayedTasks()
        {
            var filtered = AllTasks.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(t =>
                    t.Name.ToLower().Contains(searchLower) ||
                    (t.ReminderLabel ?? "").ToLower().Contains(searchLower)
                );
            }

            var sortedTasks = filtered
                .OrderBy(t => t.GetFullDueDateTime() ?? DateTime.MaxValue)
                .ToList();

            var totalCount = sortedTasks.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            
            // Ensure current page is valid
            if (CurrentPage > totalPages)
            {
                CurrentPage = totalPages;
            }

            var startIndex = (CurrentPage - 1) * PageSize;
            var pageTasks = sortedTasks.Skip(startIndex).Take(PageSize).ToList();

            DisplayedTasks.Clear();
            foreach (var task in pageTasks)
            {
                DisplayedTasks.Add(task);
            }

            var startRange = totalCount == 0 ? 0 : startIndex + 1;
            var endRange = Math.Min(totalCount, startIndex + pageTasks.Count);
            PaginationText = $"{startRange} - {endRange} of {totalCount}";
        }

        private bool CanGoNextPage()
        {
            var filtered = AllTasks.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(t =>
                    t.Name.ToLower().Contains(searchLower) ||
                    (t.ReminderLabel ?? "").ToLower().Contains(searchLower)
                );
            }
            var totalCount = filtered.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            return CurrentPage < totalPages;
        }

        private void ChangePage(int offset)
        {
            CurrentPage += offset;
        }

        private void OpenAddTaskModal()
        {
            EditingTask = new TaskItem
            {
                DueDate = DateTime.Today,
                DueTime = new TimeSpan(9, 0, 0),
                ReminderStatus = "active"
            };
            ModalTitle = "Add Task";
            IsModalOpen = true;
        }

        private void OpenEditTaskModal(TaskItem task)
        {
            if (task == null) return;

            EditingTask = new TaskItem
            {
                Id = task.Id,
                Name = task.Name,
                DueDate = task.DueDate,
                DueTime = task.DueTime,
                ReminderStatus = task.ReminderStatus,
                ReminderLabel = task.ReminderLabel,
                ExternalId = task.ExternalId,
                CreatedAt = task.CreatedAt
            };
            ModalTitle = "Edit Task";
            IsModalOpen = true;
        }

        private void SaveTask()
        {
            if (EditingTask == null || string.IsNullOrWhiteSpace(EditingTask.Name))
            {
                MessageBox.Show("Please enter a task name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Compute ReminderLabel from ReminderStatus
            if (EditingTask.ReminderStatus == "active")
            {
                EditingTask.ReminderLabel = GetReminderLabelForActive(EditingTask);
            }
            else if (EditingTask.ReminderStatus == "overdue")
            {
                EditingTask.ReminderLabel = "Overdue";
            }
            else
            {
                EditingTask.ReminderLabel = "Not set";
            }

            var existingTask = AllTasks.FirstOrDefault(t => t.Id == EditingTask.Id);

            if (existingTask != null)
            {
                // Update existing task
                existingTask.Name = EditingTask.Name;
                existingTask.DueDate = EditingTask.DueDate;
                existingTask.DueTime = EditingTask.DueTime;
                existingTask.ReminderStatus = EditingTask.ReminderStatus;
                existingTask.ReminderLabel = EditingTask.ReminderLabel;
            }
            else
            {
                // Add new task
                AllTasks.Add(EditingTask);
            }

            _state.Tasks = AllTasks.ToList();
            SaveState();

            RefreshUpcomingTask();
            GenerateCalendarDays();
            RefreshDailyTasks();
            RefreshDisplayedTasks();

            CloseModal();
        }

        private string GetReminderLabelForActive(TaskItem task)
        {
            if (task.DueDate.HasValue && task.DueTime.HasValue)
            {
                var dayOfWeek = task.DueDate.Value.ToString("dddd", CultureInfo.InvariantCulture);
                var monthDay = task.DueDate.Value.ToString("MMM dd", CultureInfo.InvariantCulture);
                var time = task.DueTime.Value.ToString(@"h\:mm");
                var amPm = task.DueTime.Value.Hours >= 12 ? "PM" : "AM";
                return $"{dayOfWeek}, {monthDay} - {time} {amPm}";
            }
            return "Active";
        }

        private void DeleteTask(TaskItem task)
        {
            if (task == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{task.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                AllTasks.Remove(task);
                _state.Tasks = AllTasks.ToList();
                SaveState();

                RefreshUpcomingTask();
                GenerateCalendarDays();
                RefreshDailyTasks();
                RefreshDisplayedTasks();
            }
        }

        private void CloseModal()
        {
            IsModalOpen = false;
            EditingTask = null;
        }

        private void OpenSyncModal()
        {
            SyncUrl = string.Empty;
            IsSyncModalOpen = true;
        }

        private void CloseSyncModal()
        {
            IsSyncModalOpen = false;
            SyncUrl = string.Empty;
        }

        private async void ImportSyncUrl()
        {
            if (string.IsNullOrWhiteSpace(SyncUrl))
            {
                MessageBox.Show("Please enter a calendar URL.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var trimmedUrl = SyncUrl.Trim();
            SyncUrl = trimmedUrl;
            if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show("Please provide a valid web address to your calendar.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                LoggingService.Log($"Starting calendar import from URL: {uri}");

                var importedTasks = await FetchCalendarTasksAsync(uri.ToString());
                if (importedTasks.Count == 0)
                {
                    MessageBox.Show("We couldn't find any events at that address. Please double-check the link and try again.", "Sync Calendar", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var added = 0;
                var updated = 0;

                foreach (var imported in importedTasks)
                {
                    if (imported == null || string.IsNullOrWhiteSpace(imported.Name))
                    {
                        continue;
                    }

                    TaskItem existing = null;

                    if (!string.IsNullOrWhiteSpace(imported.ExternalId))
                    {
                        existing = AllTasks.FirstOrDefault(t =>
                            !string.IsNullOrWhiteSpace(t.ExternalId) &&
                            string.Equals(t.ExternalId, imported.ExternalId, StringComparison.OrdinalIgnoreCase));
                    }

                    if (existing == null)
                    {
                        existing = AllTasks.FirstOrDefault(t =>
                            string.Equals(t.Name, imported.Name, StringComparison.OrdinalIgnoreCase) &&
                            Nullable.Equals(t.DueDate, imported.DueDate) &&
                            Nullable.Equals(t.DueTime, imported.DueTime));
                    }

                    if (existing != null)
                    {
                        existing.Name = imported.Name;
                        existing.DueDate = imported.DueDate;
                        existing.DueTime = imported.DueTime;
                        existing.ReminderStatus = imported.ReminderStatus;
                        existing.ReminderLabel = imported.ReminderLabel;
                        existing.ExternalId = imported.ExternalId;
                        updated++;
                    }
                    else
                    {
                        AllTasks.Add(imported);
                        added++;
                    }
                }

                _state.Tasks = AllTasks.ToList();
                SaveState();

                RefreshUpcomingTask();
                GenerateCalendarDays();
                RefreshDailyTasks();
                RefreshDisplayedTasks();

                CloseCalendarUrlModal();

                LoggingService.Log($"Calendar import completed. Added: {added}, Updated: {updated}");

                var message = added == 0 && updated == 0
                    ? "Your tasks are already up to date with the calendar."
                    : $"Calendar import complete. Added {added} and updated {updated} events.";

                MessageBox.Show(message, "Sync Calendar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to import calendar", ex);
                MessageBox.Show("We couldn't import your calendar. Please verify the URL and try again.", "Sync Calendar", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenCalendarUrlModal()
        {
            SyncUrl = string.Empty;
            IsCalendarUrlModalOpen = true;
        }

        private void CloseCalendarUrlModal()
        {
            IsCalendarUrlModalOpen = false;
            SyncUrl = string.Empty;
        }

        private async Task<List<TaskItem>> FetchCalendarTasksAsync(string url)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(15);
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return ParseCalendarEvents(content);
            }
        }

        private List<TaskItem> ParseCalendarEvents(string icsContent)
        {
            var tasks = new List<TaskItem>();

            if (string.IsNullOrWhiteSpace(icsContent))
            {
                return tasks;
            }

            var lines = UnfoldIcsLines(icsContent);

            TaskItem currentTask = null;
            DateTime? startDateTime = null;
            bool isDateOnly = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.StartsWith("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
                {
                    currentTask = new TaskItem
                    {
                        ReminderStatus = "active"
                    };
                    startDateTime = null;
                    isDateOnly = false;
                    continue;
                }

                if (line.StartsWith("END:VEVENT", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentTask != null)
                    {
                        if (startDateTime.HasValue)
                        {
                            currentTask.DueDate = startDateTime.Value.Date;
                            if (!isDateOnly)
                            {
                                currentTask.DueTime = startDateTime.Value.TimeOfDay;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(currentTask.Name))
                        {
                            currentTask.Name = "Untitled event";
                        }

                        if (currentTask.DueDate.HasValue && currentTask.DueTime.HasValue)
                        {
                            currentTask.ReminderLabel = GetReminderLabelForActive(currentTask);
                        }
                        else if (currentTask.DueDate.HasValue)
                        {
                            currentTask.ReminderLabel = currentTask.DueDate.Value.ToString("dddd, MMM dd", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            currentTask.ReminderLabel = "Active";
                        }

                        tasks.Add(currentTask);
                    }

                    currentTask = null;
                    startDateTime = null;
                    isDateOnly = false;
                    continue;
                }

                if (currentTask == null)
                {
                    continue;
                }

                if (line.StartsWith("SUMMARY", StringComparison.OrdinalIgnoreCase))
                {
                    currentTask.Name = DecodeIcsText(ExtractIcsValue(line));
                }
                else if (line.StartsWith("UID", StringComparison.OrdinalIgnoreCase))
                {
                    currentTask.ExternalId = ExtractIcsValue(line);
                }
                else if (line.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase))
                {
                    var parsed = ParseIcsDateTime(line);
                    startDateTime = parsed.DateTime;
                    isDateOnly = parsed.IsDateOnly;
                }
            }

            return tasks;
        }

        private static List<string> UnfoldIcsLines(string icsContent)
        {
            var normalized = new List<string>();
            if (string.IsNullOrEmpty(icsContent))
            {
                return normalized;
            }

            var rawLines = icsContent.Replace("\r\n", "\n").Split('\n');
            foreach (var rawLine in rawLines)
            {
                if ((rawLine.StartsWith(" ") || rawLine.StartsWith("\t")) && normalized.Count > 0)
                {
                    normalized[normalized.Count - 1] += rawLine.Substring(1);
                }
                else
                {
                    normalized.Add(rawLine);
                }
            }

            return normalized;
        }

        private static string ExtractIcsValue(string line)
        {
            var colonIndex = line.IndexOf(':');
            return colonIndex >= 0 ? line.Substring(colonIndex + 1).Trim() : string.Empty;
        }

        private static string DecodeIcsText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\n", " ")
                .Replace("\\N", " ")
                .Replace("\\,", ",")
                .Replace("\\;", ";")
                .Replace("\\\\", "\\")
                .Trim();
        }

        private (DateTime? DateTime, bool IsDateOnly) ParseIcsDateTime(string line)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex < 0)
            {
                return (null, false);
            }

            var metadata = line.Substring(0, colonIndex);
            var valuePart = line.Substring(colonIndex + 1).Trim();
            var isDateOnly = metadata.IndexOf("VALUE=DATE", StringComparison.OrdinalIgnoreCase) >= 0 || valuePart.Length == 8;

            if (isDateOnly)
            {
                if (DateTime.TryParseExact(valuePart, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
                {
                    return (dateOnly, true);
                }

                return (null, true);
            }

            if (valuePart.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            {
                if (DateTime.TryParseExact(valuePart, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var utcDateTime))
                {
                    return (utcDateTime.ToLocalTime(), false);
                }

                if (DateTime.TryParseExact(valuePart, "yyyyMMdd'T'HHmm'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out utcDateTime))
                {
                    return (utcDateTime.ToLocalTime(), false);
                }
            }

            var formats = new[] { "yyyyMMdd'T'HHmmss", "yyyyMMdd'T'HHmm" };
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(valuePart, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var localDateTime))
                {
                    return (localDateTime, false);
                }
            }

            return (null, false);
        }

        private void SaveState()
        {
            _state.Tasks = AllTasks.ToList();
            _state.CurrentPage = CurrentPage;
            _state.PageSize = PageSize;
            _storageService.SaveState(_state);
        }
    }

    // Helper class for calendar display
    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public int Day { get; set; }
        public bool IsPlaceholder { get; set; }
        public bool HasTasks { get; set; }
        public bool IsToday { get; set; }
        public bool IsSelected { get; set; }
    }
}
