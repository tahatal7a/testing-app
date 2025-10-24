using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DesktopTaskAid.Helpers;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;
using Microsoft.Win32;

namespace DesktopTaskAid.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly StorageService _storageService;
        private readonly CalendarImportService _calendarImportService;
        private readonly DispatcherTimer _timerTick;
        private readonly RelayCommand _importNextMonthCommand;
        private readonly RelayCommand _openImportModalCommand;
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

        private string _importStatusMessage;
        public string ImportStatusMessage
        {
            get => _importStatusMessage;
            set => SetProperty(ref _importStatusMessage, value);
        }

        private bool _isImportRunning;
        public bool IsImportRunning
        {
            get => _isImportRunning;
            private set
            {
                if (SetProperty(ref _isImportRunning, value))
                {
                    _importNextMonthCommand?.RaiseCanExecuteChanged();
                    _openImportModalCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _hasValidCredentials;
        public bool HasValidCredentials
        {
            get => _hasValidCredentials;
            private set
            {
                if (SetProperty(ref _hasValidCredentials, value))
                {
                    _importNextMonthCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isCalendarImportModalOpen;
        public bool IsCalendarImportModalOpen
        {
            get => _isCalendarImportModalOpen;
            set => SetProperty(ref _isCalendarImportModalOpen, value);
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
        public ICommand ImportNextMonthCommand => _importNextMonthCommand;
        public ICommand CreateGoogleAccountCommand { get; }
        public ICommand OpenCalendarImportModalCommand => _openImportModalCommand;
        public ICommand CloseCalendarImportModalCommand { get; }

        #endregion

        public MainViewModel()
        {
            LoggingService.Log("=== MainViewModel Constructor BEGIN ===");
            
            try
            {
                LoggingService.Log("Creating StorageService");
                _storageService = new StorageService();
                _calendarImportService = new CalendarImportService(_storageService);
                _calendarImportService.CredentialsChanged += CalendarImportServiceOnCredentialsChanged;
                
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

                // Surface a theme flag for converters/resources
                try
                {
                    if (Application.Current != null)
                    {
                        Application.Current.Resources["IsDarkTheme"] = IsDarkTheme;
                    }
                }
                catch { }

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
                _openImportModalCommand = new RelayCommand(_ => OpenCalendarImportModal(), _ => !IsImportRunning);
                _importNextMonthCommand = new RelayCommand(async _ => await RunImportAsync(), _ => !IsImportRunning);
                CloseCalendarImportModalCommand = new RelayCommand(_ => CloseCalendarImportModal());
                CreateGoogleAccountCommand = new RelayCommand(_ => OpenGoogleAccountPage());
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

                ApplyCredentialState(_calendarImportService.GetCredentialState());

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
            try
            {
                if (Application.Current != null)
                {
                    Application.Current.Resources["IsDarkTheme"] = IsDarkTheme;
                    Application.Current.Resources["ThemeName"] = CurrentTheme; // helpers for converters in tests

                    // In unit tests, avoid loading resource dictionaries from pack URIs
                    if (!IsRunningUnderUnitTest())
                    {
                        Application.Current.Resources.MergedDictionaries.Clear();
                        var themeDict = new ResourceDictionary
                        {
                            Source = new Uri($"pack://application:,,,/Themes/{CurrentTheme}Theme.xaml")
                        };
                        Application.Current.Resources.MergedDictionaries.Add(themeDict);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log and continue; UI can still rely on simple resources
                LoggingService.LogError("Failed to apply theme resources", ex);
            }
            
            // Notify listeners (e.g., MainWindow) that theme has changed
            ThemeChanged?.Invoke();
        }

        // Utility: detect running under unit tests to avoid pack URI operations
        private static bool IsRunningUnderUnitTest()
        {
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                return asms.Any(a =>
                    (a.FullName?.IndexOf("nunit", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (a.FullName?.IndexOf("xunit", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (a.FullName?.IndexOf("mstest", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
            }
            catch { return false; }
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

        private void CalendarImportServiceOnCredentialsChanged(object sender, EventArgs e)
        {
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            dispatcher.Invoke(() => ApplyCredentialState(_calendarImportService.GetCredentialState()));
        }

        private void ApplyCredentialState(CredentialState state)
        {
            if (state == null)
            {
                return;
            }

            HasValidCredentials = state.Status == CredentialStatus.Valid;
            ImportStatusMessage = state.Message;
        }

        private async Task<bool> EnsureCredentialsAvailableAsync()
        {
            var state = _calendarImportService.GetCredentialState();
            ApplyCredentialState(state);

            if (state.Status == CredentialStatus.Valid)
            {
                return true;
            }

            return await PromptForCredentialsAsync();
        }

        private async Task<bool> PromptForCredentialsAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select google-credentials.json"
            };

            var result = dialog.ShowDialog();
            if (result == true)
            {
                ImportStatusMessage = "Validating google-credentials.json...";
                var state = await _calendarImportService.ImportCredentialsAsync(dialog.FileName);
                ApplyCredentialState(state);
                return state.Status == CredentialStatus.Valid;
            }

            ImportStatusMessage = "Import canceled. Select google-credentials.json to continue.";
            return false;
        }

        private void OpenCalendarImportModal()
        {
            if (IsImportRunning)
            {
                return;
            }

            IsCalendarImportModalOpen = true;
        }

        private void CloseCalendarImportModal()
        {
            IsCalendarImportModalOpen = false;
        }

        private async Task RunImportAsync()
        {
            if (IsImportRunning)
            {
                return;
            }

            try
            {
                LoggingService.Log("ImportNextMonth command started");

                if (IsCalendarImportModalOpen)
                {
                    CloseCalendarImportModal();
                }

                if (!await EnsureCredentialsAvailableAsync())
                {
                    LoggingService.Log("ImportNextMonth canceled - credentials not available");
                    return;
                }

                IsImportRunning = true;
                ImportStatusMessage = "Connecting to Google Calendar...";
                LoggingService.Log("Calling CalendarImportService.RunImportAsync");

                var result = await _calendarImportService.RunImportAsync();
                await HandleImportResultAsync(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Unexpected error during Google Calendar import", ex);
                ImportStatusMessage = "We couldn't import your Google Calendar. Please try again.";
            }
            finally
            {
                IsImportRunning = false;
            }
        }

        private Task HandleImportResultAsync(CalendarImportResult result)
        {
            if (result == null)
            {
                ImportStatusMessage = "We couldn't import your Google Calendar. Please try again.";
                return Task.CompletedTask;
            }

            switch (result.Outcome)
            {
                case CalendarImportOutcome.Success:
                    var mergeResult = MergeImportedTasks(result.Tasks);
                    ImportStatusMessage = BuildImportSummaryMessage(mergeResult);
                    break;
                case CalendarImportOutcome.NoEvents:
                    ImportStatusMessage = "No Google Calendar events were found for next month.";
                    break;
                case CalendarImportOutcome.Cancelled:
                    ImportStatusMessage = "Sign-in canceled. Run Import Next Month when you're ready.";
                    break;
                case CalendarImportOutcome.AccessBlocked:
                    ImportStatusMessage = "Google blocked the request. Add your account as a test user on the OAuth consent screen.";
                    break;
                case CalendarImportOutcome.MissingCredentials:
                case CalendarImportOutcome.InvalidCredentials:
                    ApplyCredentialState(_calendarImportService.GetCredentialState());
                    ImportStatusMessage = result.ErrorMessage;
                    break;
                default:
                    ImportStatusMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "We couldn't import your Google Calendar. Please try again."
                        : result.ErrorMessage;
                    break;
            }

            return Task.CompletedTask;
        }

        private MergeResult MergeImportedTasks(IEnumerable<TaskItem> importedTasks)
        {
            if (importedTasks == null)
            {
                return new MergeResult(0, 0, 0);
            }

            var added = 0;
            var updated = 0;
            var duplicates = 0;

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
                    if (ApplyImportedValues(existing, imported))
                    {
                        updated++;
                    }
                    else
                    {
                        duplicates++;
                    }

                    continue;
                }

                AllTasks.Add(imported);
                added++;
            }

            _state.Tasks = AllTasks.ToList();
            SaveState();

            RefreshUpcomingTask();
            GenerateCalendarDays();
            RefreshDailyTasks();
            RefreshDisplayedTasks();

            LoggingService.Log($"Google import merge result - Added: {added}, Updated: {updated}, Duplicates: {duplicates}");

            return new MergeResult(added, updated, duplicates);
        }

        private bool ApplyImportedValues(TaskItem existing, TaskItem imported)
        {
            var changed = false;

            if (!string.Equals(existing.Name, imported.Name, StringComparison.Ordinal))
            {
                existing.Name = imported.Name;
                changed = true;
            }

            if (!Nullable.Equals(existing.DueDate, imported.DueDate))
            {
                existing.DueDate = imported.DueDate;
                changed = true;
            }

            if (!Nullable.Equals(existing.DueTime, imported.DueTime))
            {
                existing.DueTime = imported.DueTime;
                changed = true;
            }

            if (!string.Equals(existing.ReminderStatus, imported.ReminderStatus, StringComparison.OrdinalIgnoreCase))
            {
                existing.ReminderStatus = imported.ReminderStatus;
                changed = true;
            }

            if (!string.Equals(existing.ReminderLabel, imported.ReminderLabel, StringComparison.Ordinal))
            {
                existing.ReminderLabel = imported.ReminderLabel;
                changed = true;
            }

            if (!string.Equals(existing.ExternalId, imported.ExternalId, StringComparison.OrdinalIgnoreCase))
            {
                existing.ExternalId = imported.ExternalId;
                changed = true;
            }

            return changed;
        }

        private string BuildImportSummaryMessage(MergeResult mergeResult)
        {
            if (!mergeResult.HasChanges && mergeResult.Duplicates > 0)
            {
                return $"You're already up to date. Skipped {mergeResult.Duplicates} duplicate {(mergeResult.Duplicates == 1 ? "event" : "events")}.";
            }

            if (!mergeResult.HasChanges)
            {
                return "No new Google Calendar events to import for next month.";
            }

            var parts = new List<string>();

            if (mergeResult.Added > 0)
            {
                parts.Add($"{mergeResult.Added} new {(mergeResult.Added == 1 ? "event" : "events")}");
            }

            if (mergeResult.Updated > 0)
            {
                parts.Add($"{mergeResult.Updated} {(mergeResult.Updated == 1 ? "task updated" : "tasks updated")}");
            }

            if (mergeResult.Duplicates > 0)
            {
                parts.Add($"{mergeResult.Duplicates} duplicate{(mergeResult.Duplicates == 1 ? string.Empty : "s")} skipped");
            }

            return $"Import complete: {string.Join(", ", parts)}.";
        }

        private void OpenGoogleAccountPage()
        {
            const string signupUrl = "https://accounts.google.com/signup";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = signupUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to open Google account sign-up page", ex);
                MessageBox.Show($"We couldn't open the sign-up page automatically. Visit {signupUrl} in your browser.", "Google Calendar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private readonly struct MergeResult
        {
            public MergeResult(int added, int updated, int duplicates)
            {
                Added = added;
                Updated = updated;
                Duplicates = duplicates;
            }

            public int Added { get; }
            public int Updated { get; }
            public int Duplicates { get; }
            public bool HasChanges => Added > 0 || Updated > 0;
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
