using DesktopTaskAid.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopTaskAid.Services
{
    public enum CredentialStatus
    {
        Missing,
        Valid,
        Invalid
    }

    public enum CalendarImportOutcome
    {
        Success,
        NoEvents,
        Cancelled,
        AccessBlocked,
        MissingCredentials,
        InvalidCredentials,
        Error
    }

    public class CredentialState
    {
        public CredentialStatus Status { get; set; }
        public string Message { get; set; }
    }

    public class CalendarImportResult
    {
        public CalendarImportOutcome Outcome { get; set; }
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public string ErrorMessage { get; set; }
    }

    public interface IGoogleCalendarClient
    {
        Task<IList<Event>> FetchEventsAsync(Stream credentialStream, string tokenDirectory, DateTime timeMin, DateTime timeMax, CancellationToken cancellationToken);
    }

    public sealed class CalendarImportService : IDisposable
    {
        private const string CredentialFileName = "google-credentials.json";
        private const string ApplicationName = "DesktopTaskAid";
        private static readonly string[] Scopes = { CalendarService.Scope.CalendarReadonly };

        private readonly StorageService _storageService;
        private readonly string _appDirectory;
        private readonly string _credentialsPath;
        private readonly string _tokenDirectory;
        private readonly IGoogleCalendarClient _calendarClient;
        private readonly bool _enableWatcher;
        private readonly object _stateLock = new object();
        private FileSystemWatcher _watcher;
        private CredentialState _credentialState;

        public event EventHandler CredentialsChanged;

        public CalendarImportService(StorageService storageService, IGoogleCalendarClient calendarClient = null, string appDirectory = null, bool enableWatcher = true)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _calendarClient = calendarClient ?? new GoogleCalendarClient();
            _appDirectory = string.IsNullOrWhiteSpace(appDirectory)
                ? AppDomain.CurrentDomain.BaseDirectory
                : appDirectory;
            _credentialsPath = Path.Combine(_appDirectory, CredentialFileName);
            _tokenDirectory = Path.Combine(_storageService.GetDataFolderPath(), "GoogleOAuth");
            _enableWatcher = enableWatcher;

            Directory.CreateDirectory(_appDirectory);
            Directory.CreateDirectory(_tokenDirectory);

            _credentialState = ValidateCredentialFile();

            if (_enableWatcher)
            {
                InitializeWatcher();
            }
        }

        public CredentialState GetCredentialState()
        {
            lock (_stateLock)
            {
                return new CredentialState
                {
                    Status = _credentialState.Status,
                    Message = _credentialState.Message
                };
            }
        }

        public async Task<CredentialState> ImportCredentialsAsync(string sourcePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                return UpdateCredentialState(new CredentialState
                {
                    Status = CredentialStatus.Missing,
                    Message = "We couldn't find that google-credentials.json file."
                });
            }

            try
            {
                var json = await ReadAllTextAsync(sourcePath, cancellationToken).ConfigureAwait(false);
                if (!ValidateCredentialJson(json, out var validationMessage))
                {
                    return UpdateCredentialState(new CredentialState
                    {
                        Status = CredentialStatus.Invalid,
                        Message = validationMessage
                    });
                }

                await WriteAllTextAsync(_credentialsPath, json, cancellationToken).ConfigureAwait(false);
                ClearCachedTokens();

                var message = "Credentials saved. Click Import Next Month to sign in with Google.";
                return UpdateCredentialState(new CredentialState
                {
                    Status = CredentialStatus.Valid,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to import google-credentials.json", ex);
                return UpdateCredentialState(new CredentialState
                {
                    Status = CredentialStatus.Invalid,
                    Message = "We couldn't save google-credentials.json. Try picking the file again."
                });
            }
        }

        public async Task<CalendarImportResult> RunImportAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            CredentialState credentialState;
            lock (_stateLock)
            {
                credentialState = _credentialState;
            }

            if (credentialState.Status == CredentialStatus.Missing)
            {
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.MissingCredentials,
                    ErrorMessage = credentialState.Message
                };
            }

            if (credentialState.Status == CredentialStatus.Invalid)
            {
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.InvalidCredentials,
                    ErrorMessage = credentialState.Message
                };
            }

            try
            {
                using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var now = DateTime.Now;
                    var nextMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local).AddMonths(1);
                    var nextMonthEnd = nextMonthStart.AddMonths(1);

                    var events = await _calendarClient
                        .FetchEventsAsync(stream, _tokenDirectory, nextMonthStart, nextMonthEnd, cancellationToken)
                        .ConfigureAwait(false);

                    if (events == null)
                    {
                        return new CalendarImportResult
                        {
                            Outcome = CalendarImportOutcome.Cancelled,
                            ErrorMessage = "Authorization was canceled."
                        };
                    }

                    var eventList = events.ToList();
                    if (eventList.Count == 0)
                    {
                        return new CalendarImportResult { Outcome = CalendarImportOutcome.NoEvents };
                    }

                    var tasks = eventList
                        .Select(ConvertToTaskItem)
                        .Where(t => t != null)
                        .ToList();

                    return new CalendarImportResult
                    {
                        Outcome = CalendarImportOutcome.Success,
                        Tasks = tasks
                    };
                }
            }
            catch (TaskCanceledException)
            {
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.Cancelled,
                    ErrorMessage = "Authorization was canceled."
                };
            }
            catch (TokenResponseException ex) when (string.Equals(ex.Error?.Error, "access_denied", StringComparison.OrdinalIgnoreCase))
            {
                LoggingService.Log("User canceled the authorization request.", "WARN");
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.Cancelled,
                    ErrorMessage = "Authorization was canceled."
                };
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Forbidden)
            {
                LoggingService.LogError("Google Calendar returned access denied.", ex);
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.AccessBlocked,
                    ErrorMessage = "Google blocked the request. Add your account as a test user and try again."
                };
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Unexpected error importing Google Calendar events.", ex);
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.Error,
                    ErrorMessage = ex.Message
                };
            }
        }

        private static Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return File.ReadAllText(path);
            }, cancellationToken);
        }

        private static Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.WriteAllText(path, contents);
            }, cancellationToken);
        }

        private TaskItem ConvertToTaskItem(Event calendarEvent)
        {
            if (calendarEvent == null)
            {
                return null;
            }

            var startInfo = GetStartInfo(calendarEvent.Start);
            var task = new TaskItem
            {
                Name = string.IsNullOrWhiteSpace(calendarEvent.Summary) ? "Untitled event" : calendarEvent.Summary.Trim(),
                DueDate = startInfo.Date,
                DueTime = startInfo.Time,
                ReminderStatus = "none",
                ReminderLabel = "Not set",
                ExternalId = calendarEvent.Id
            };

            if (calendarEvent.Reminders?.Overrides != null && calendarEvent.Reminders.Overrides.Count > 0)
            {
                var overrideReminder = calendarEvent.Reminders.Overrides
                    .Where(r => r.Minutes.HasValue)
                    .OrderBy(r => r.Minutes.Value)
                    .FirstOrDefault();

                if (overrideReminder?.Minutes != null)
                {
                    task.ReminderStatus = "active";
                    task.ReminderLabel = FormatReminderMinutes(overrideReminder.Minutes.Value);
                }
            }
            else if (calendarEvent.Reminders?.UseDefault == true)
            {
                task.ReminderStatus = "active";
                task.ReminderLabel = "Default reminder";
            }
            else if (startInfo.Date.HasValue)
            {
                task.ReminderStatus = "active";
                task.ReminderLabel = BuildDefaultReminderLabel(startInfo);
            }

            return task;
        }

        private EventStartInfo GetStartInfo(EventDateTime start)
        {
            if (start == null)
            {
                return new EventStartInfo(null, null, false);
            }

            if (!string.IsNullOrEmpty(start.DateTimeRaw))
            {
                if (DateTimeOffset.TryParse(start.DateTimeRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                {
                    var local = dto.ToLocalTime();
                    return new EventStartInfo(local.Date, local.TimeOfDay, false);
                }
            }

            if (start.DateTime.HasValue)
            {
                var dateTime = start.DateTime.Value;

                if (!string.IsNullOrEmpty(start.TimeZone))
                {
                    try
                    {
                        var sourceZone = TimeZoneInfo.FindSystemTimeZoneById(start.TimeZone);
                        var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
                        var converted = TimeZoneInfo.ConvertTime(unspecified, sourceZone, TimeZoneInfo.Local);
                        return new EventStartInfo(converted.Date, converted.TimeOfDay, false);
                    }
                    catch (TimeZoneNotFoundException)
                    {
                    }
                    catch (InvalidTimeZoneException)
                    {
                    }
                }

                if (dateTime.Kind == DateTimeKind.Utc)
                {
                    dateTime = dateTime.ToLocalTime();
                }
                else if (dateTime.Kind == DateTimeKind.Unspecified)
                {
                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                }

                return new EventStartInfo(dateTime.Date, dateTime.TimeOfDay, false);
            }

            if (!string.IsNullOrEmpty(start.Date))
            {
                if (DateTime.TryParseExact(start.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    return new EventStartInfo(date.Date, null, true);
                }
            }

            return new EventStartInfo(null, null, false);
        }

        private string BuildDefaultReminderLabel(EventStartInfo startInfo)
        {
            if (startInfo.Date.HasValue && startInfo.Time.HasValue)
            {
                var dayOfWeek = startInfo.Date.Value.ToString("dddd", CultureInfo.InvariantCulture);
                var monthDay = startInfo.Date.Value.ToString("MMM dd", CultureInfo.InvariantCulture);
                var time = DateTime.Today.Add(startInfo.Time.Value).ToString("h:mm tt", CultureInfo.InvariantCulture);
                return $"{dayOfWeek}, {monthDay} - {time}";
            }

            if (startInfo.Date.HasValue)
            {
                return startInfo.Date.Value.ToString("dddd, MMM dd", CultureInfo.InvariantCulture);
            }

            return "Active";
        }

        private string FormatReminderMinutes(int minutes)
        {
            if (minutes == 0)
            {
                return "At start time";
            }

            if (minutes == 1)
            {
                return "1 minute before";
            }

            if (minutes < 60)
            {
                return $"{minutes} minutes before";
            }

            if (minutes == 60)
            {
                return "1 hour before";
            }

            if (minutes < 1440 && minutes % 60 == 0)
            {
                var hours = minutes / 60;
                return $"{hours} hours before";
            }

            if (minutes == 1440)
            {
                return "1 day before";
            }

            if (minutes % 1440 == 0)
            {
                var days = minutes / 1440;
                return $"{days} days before";
            }

            var timeSpan = TimeSpan.FromMinutes(minutes);
            var parts = new List<string>();
            if (timeSpan.Days > 0)
            {
                parts.Add($"{timeSpan.Days} day{(timeSpan.Days == 1 ? string.Empty : "s")}");
            }

            if (timeSpan.Hours > 0)
            {
                parts.Add($"{timeSpan.Hours} hour{(timeSpan.Hours == 1 ? string.Empty : "s")}");
            }

            if (timeSpan.Minutes > 0)
            {
                parts.Add($"{timeSpan.Minutes} minute{(timeSpan.Minutes == 1 ? string.Empty : "s")}");
            }

            if (parts.Count == 0)
            {
                parts.Add($"{minutes} minutes");
            }

            return string.Join(" ", parts) + " before";
        }

        private CredentialState ValidateCredentialFile()
        {
            if (!File.Exists(_credentialsPath))
            {
                var autoImportState = TryAutoImportCredentials();
                if (autoImportState != null)
                {
                    return autoImportState;
                }

                return new CredentialState
                {
                    Status = CredentialStatus.Missing,
                    Message = "Add google-credentials.json next to the app to import upcoming events."
                };
            }

            try
            {
                var json = File.ReadAllText(_credentialsPath);
                if (ValidateCredentialJson(json, out var message))
                {
                    return new CredentialState
                    {
                        Status = CredentialStatus.Valid,
                        Message = "google-credentials.json looks good. Click Import Next Month to continue."
                    };
                }

                return new CredentialState
                {
                    Status = CredentialStatus.Invalid,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to validate google-credentials.json", ex);
                return new CredentialState
                {
                    Status = CredentialStatus.Invalid,
                    Message = "We couldn't read google-credentials.json. Choose the file again."
                };
            }
        }

        private CredentialState TryAutoImportCredentials()
        {
            var candidatePath = FindCredentialFileCandidate();
            if (string.IsNullOrEmpty(candidatePath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(candidatePath);
                if (!ValidateCredentialJson(json, out var validationMessage))
                {
                    return new CredentialState
                    {
                        Status = CredentialStatus.Invalid,
                        Message = validationMessage
                    };
                }

                File.WriteAllText(_credentialsPath, json);
                ClearCachedTokens();

                return new CredentialState
                {
                    Status = CredentialStatus.Valid,
                    Message = "google-credentials.json found. Click Import Next Month to continue."
                };
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to automatically import google-credentials.json", ex);
                return new CredentialState
                {
                    Status = CredentialStatus.Invalid,
                    Message = "We couldn't read google-credentials.json. Choose the file again."
                };
            }
        }

        private string FindCredentialFileCandidate()
        {
            try
            {
                var directory = new DirectoryInfo(_appDirectory);
                for (var depth = 0; depth < 5 && directory != null; depth++)
                {
                    var exactPath = Path.Combine(directory.FullName, CredentialFileName);
                    if (File.Exists(exactPath))
                    {
                        return exactPath;
                    }

                    var alternate = GetAlternateCredentialPath(directory.FullName);
                    if (!string.IsNullOrEmpty(alternate))
                    {
                        return alternate;
                    }

                    directory = directory.Parent;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to search for google-credentials.json automatically", ex);
            }

            return null;
        }

        private static string GetAlternateCredentialPath(string directory)
        {
            try
            {
                var files = Directory.GetFiles(directory, CredentialFileName + "*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    if (fileName != null && fileName.StartsWith(CredentialFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        return file;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to enumerate potential credential files", ex);
            }

            return null;
        }

        private bool ValidateCredentialJson(string json, out string message)
        {
            try
            {
                var jObject = JObject.Parse(json);
                var root = (JObject)jObject["installed"] ?? (JObject)jObject["web"];
                if (root == null)
                {
                    message = "The credential file must include an \"installed\" client configuration.";
                    return false;
                }

                var clientId = root.Value<string>("client_id");
                var clientSecret = root.Value<string>("client_secret");
                var redirectUris = root["redirect_uris"] as JArray;

                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                {
                    message = "The credential file is missing the client ID or secret.";
                    return false;
                }

                if (redirectUris == null || redirectUris.Count == 0)
                {
                    message = "The credential file is missing redirect URIs.";
                    return false;
                }

                message = "Credentials look good. Continue with Import Next Month.";
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Invalid google-credentials.json", ex);
                message = "google-credentials.json isn't valid JSON. Download a fresh file and try again.";
                return false;
            }
        }

        private CredentialState UpdateCredentialState(CredentialState newState)
        {
            lock (_stateLock)
            {
                _credentialState = new CredentialState
                {
                    Status = newState.Status,
                    Message = newState.Message
                };
            }

            CredentialsChanged?.Invoke(this, EventArgs.Empty);
            return GetCredentialState();
        }

        private void InitializeWatcher()
        {
            try
            {
                _watcher = new FileSystemWatcher(_appDirectory, CredentialFileName)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
                };

                _watcher.Changed += HandleCredentialsFileChanged;
                _watcher.Created += HandleCredentialsFileChanged;
                _watcher.Renamed += HandleCredentialsFileChanged;
                _watcher.Deleted += HandleCredentialsFileChanged;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to initialize credentials watcher", ex);
            }
        }

        private void HandleCredentialsFileChanged(object sender, FileSystemEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(200).ConfigureAwait(false);

                try
                {
                    if (e.ChangeType == WatcherChangeTypes.Deleted)
                    {
                        UpdateCredentialState(new CredentialState
                        {
                            Status = CredentialStatus.Missing,
                            Message = "Add google-credentials.json next to the app to import upcoming events."
                        });
                        return;
                    }

                    var state = ValidateCredentialFile();
                    if (state.Status == CredentialStatus.Valid)
                    {
                        ClearCachedTokens();
                    }

                    UpdateCredentialState(state);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError("Error reacting to google-credentials.json changes", ex);
                }
            });
        }

        private void ClearCachedTokens()
        {
            try
            {
                if (Directory.Exists(_tokenDirectory))
                {
                    Directory.Delete(_tokenDirectory, true);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to clear Google OAuth token cache", ex);
            }
            finally
            {
                try
                {
                    Directory.CreateDirectory(_tokenDirectory);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError("Failed to recreate Google OAuth token directory", ex);
                }
            }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }

        private readonly struct EventStartInfo
        {
            public EventStartInfo(DateTime? date, TimeSpan? time, bool isAllDay)
            {
                Date = date;
                Time = time;
                IsAllDay = isAllDay;
            }

            public DateTime? Date { get; }
            public TimeSpan? Time { get; }
            public bool IsAllDay { get; }
        }

        private sealed class GoogleCalendarClient : IGoogleCalendarClient
        {
            public async Task<IList<Event>> FetchEventsAsync(Stream credentialStream, string tokenDirectory, DateTime timeMin, DateTime timeMax, CancellationToken cancellationToken)
            {
                var secrets = await GoogleClientSecrets.FromStreamAsync(credentialStream, cancellationToken).ConfigureAwait(false);

                var dataStore = new FileDataStore(tokenDirectory, true);
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    secrets.Secrets,
                    Scopes,
                    "desktop-user",
                    cancellationToken,
                    dataStore).ConfigureAwait(false);

                if (credential == null)
                {
                    return null;
                }

                var service = new CalendarService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

                var request = service.Events.List("primary");
                request.TimeMin = timeMin;
                request.TimeMax = timeMax;
                request.SingleEvents = true;
                request.ShowDeleted = false;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
                request.MaxResults = 2500;
                request.TimeZone = TimeZoneInfo.Local.Id;

                var response = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                return response?.Items ?? new List<Event>();
            }
        }
    }
}
