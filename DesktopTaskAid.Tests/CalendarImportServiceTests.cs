using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;
using Google.Apis;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3.Data;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class CalendarImportServiceTests
    {
        private const string ModuleName = "Google Calendar Import";

        private string _tempDirectory;
        private string _dataDirectory;
        private StorageService _storageService;
        private FakeCalendarClient _calendarClient;
        private CalendarImportService _service;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            _dataDirectory = Path.Combine(_tempDirectory, "data");
            Directory.CreateDirectory(_dataDirectory);

            _storageService = new StorageService(_dataDirectory);
            _calendarClient = new FakeCalendarClient();
            _service = new CalendarImportService(_storageService, _calendarClient, _tempDirectory, enableWatcher: false);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();

            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Test]
        public async Task TC_CAL_001_MissingCredentials_ReturnsMissingCredentialsOutcome()
        {
            DocumentTestCase(
                "TC_CAL_001",
                "Verify RunImportAsync returns MissingCredentials when google-credentials.json is absent",
                "Objective: Confirm the service provides actionable feedback when credentials are not available.",
                $"Module: {ModuleName}",
                "Preconditions: CalendarImportService is initialised without a credential file in the app directory.",
                "Test Data: None",
                "Test Steps:",
                "1. Invoke RunImportAsync().",
                "Expected Result: Outcome equals MissingCredentials and the Google client is not invoked.");

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.MissingCredentials, result.Outcome);
            Assert.IsFalse(_calendarClient.WasInvoked);
        }

        [Test]
        public async Task TC_CAL_002_InvalidCredentials_ReturnsInvalidCredentialsOutcome()
        {
            DocumentTestCase(
                "TC_CAL_002",
                "Validate RunImportAsync flags InvalidCredentials when the credential JSON is malformed",
                "Objective: Ensure JSON validation prevents OAuth attempts with invalid files.",
                $"Module: {ModuleName}",
                "Preconditions: A malformed google-credentials.json is imported via ImportCredentialsAsync().",
                "Test Data: 'not json' literal written to the credential file.",
                "Test Steps:",
                "1. Call ImportCredentialsAsync() with malformed JSON.",
                "2. Execute RunImportAsync().",
                "Expected Result: Outcome equals InvalidCredentials and the Google client is not invoked.");

            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            File.WriteAllText(credentialPath, "not json");

            await _service.ImportCredentialsAsync(credentialPath);

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.InvalidCredentials, result.Outcome);
            Assert.IsFalse(_calendarClient.WasInvoked);
        }

        [Test]
        public async Task TC_CAL_003_UserCancellation_ReturnsCancelledOutcome()
        {
            DocumentTestCase(
                "TC_CAL_003",
                "Ensure RunImportAsync reports Cancelled when the OAuth flow is abandoned",
                "Objective: Verify null event payloads are interpreted as user cancellation.",
                $"Module: {ModuleName}",
                "Preconditions: Valid credentials exist and the calendar client returns null.",
                "Test Data: None",
                "Test Steps:",
                "1. Persist a valid google-credentials.json.",
                "2. Configure the fake Google client to return null events.",
                "3. Execute RunImportAsync().",
                "Expected Result: Outcome equals Cancelled.");

            await WriteValidCredentialsAsync();
            _calendarClient.EventsToReturn = null;

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Cancelled, result.Outcome);
        }

        [Test]
        public async Task TC_CAL_004_NoEvents_ReturnsNoEventsOutcome()
        {
            DocumentTestCase(
                "TC_CAL_004",
                "Confirm RunImportAsync returns NoEvents when the Google API contains no data",
                "Objective: Ensure empty event collections are surfaced to the UI without errors.",
                $"Module: {ModuleName}",
                "Preconditions: Valid credentials exist and the calendar client returns an empty list.",
                "Test Data: Empty IList<Event>.",
                "Test Steps:",
                "1. Persist a valid google-credentials.json.",
                "2. Configure the fake Google client to return an empty collection.",
                "3. Execute RunImportAsync().",
                "Expected Result: Outcome equals NoEvents.");

            await WriteValidCredentialsAsync();
            _calendarClient.EventsToReturn = new List<Event>();

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.NoEvents, result.Outcome);
        }

        [Test]
        public async Task TC_CAL_005_Success_ReturnsTasksForUpcomingEvents()
        {
            DocumentTestCase(
                "TC_CAL_005",
                "Verify RunImportAsync returns Success and converts upcoming events into TaskItem records",
                "Objective: Confirm the happy path creates task entries for calendar events beyond the current month.",
                $"Module: {ModuleName}",
                "Preconditions: Valid credentials exist and the calendar client returns an event within the import window.",
                "Test Data: Single timed event with summary 'Team Sync'.",
                "Test Steps:",
                "1. Persist a valid google-credentials.json.",
                "2. Configure the fake Google client to return a single future event.",
                "3. Execute RunImportAsync().",
                "Expected Result: Outcome equals Success and the returned task mirrors the event metadata.");

            await WriteValidCredentialsAsync();

            var calendarEvent = new Event
            {
                Id = "abc123",
                Summary = "Team Sync",
                Start = new EventDateTime
                {
                    DateTime = DateTime.UtcNow.AddDays(35)
                }
            };

            _calendarClient.EventsToReturn = new List<Event> { calendarEvent };

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);
            Assert.AreEqual(1, result.Tasks.Count);
            Assert.AreEqual("abc123", result.Tasks[0].ExternalId);
            Assert.AreEqual("Team Sync", result.Tasks[0].Name);
        }

        [Test]
        public async Task TC_CAL_006_AccessBlocked_ReturnsAccessBlockedOutcome()
        {
            DocumentTestCase(
                "TC_CAL_006",
                "Ensure RunImportAsync maps HTTP 403 errors to the AccessBlocked outcome",
                "Objective: Provide clear messaging when Google rejects OAuth due to consent screen restrictions.",
                $"Module: {ModuleName}",
                "Preconditions: Valid credentials exist and the Google client throws a GoogleApiException with status 403.",
                "Test Data: GoogleApiException (Forbidden).",
                "Test Steps:",
                "1. Persist a valid google-credentials.json.",
                "2. Configure the fake Google client to throw GoogleApiException with Forbidden status.",
                "3. Execute RunImportAsync().",
                "Expected Result: Outcome equals AccessBlocked.");

            await WriteValidCredentialsAsync();

            var apiException = new GoogleApiException("Calendar", "Forbidden")
            {
                HttpStatusCode = HttpStatusCode.Forbidden
            };

            _calendarClient.ExceptionToThrow = apiException;

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.AccessBlocked, result.Outcome);
        }

        [Test]
        public async Task TC_CAL_007_AccessDeniedDuringTokenExchange_ReturnsCancelledOutcome()
        {
            DocumentTestCase(
                "TC_CAL_007",
                "Validate RunImportAsync handles access_denied token responses by reporting Cancelled",
                "Objective: Ensure OAuth rejection by the user is surfaced as a cancellation event.",
                $"Module: {ModuleName}",
                "Preconditions: Valid credentials exist and the Google client throws TokenResponseException with error access_denied.",
                "Test Data: TokenResponseException (access_denied).",
                "Test Steps:",
                "1. Persist a valid google-credentials.json.",
                "2. Configure the fake Google client to throw TokenResponseException (access_denied).",
                "3. Execute RunImportAsync().",
                "Expected Result: Outcome equals Cancelled.");

            await WriteValidCredentialsAsync();

            var tokenException = new TokenResponseException(new TokenErrorResponse
            {
                Error = "access_denied"
            });

            _calendarClient.ExceptionToThrow = tokenException;

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Cancelled, result.Outcome);
        }

        [Test]
        public async Task TC_CAL_008_UnexpectedFailure_ReturnsErrorOutcome()
        {
            DocumentTestCase(
                "TC_CAL_008",
                "Ensure RunImportAsync surfaces unexpected exceptions through the Error outcome",
                "Objective: Confirm that unhandled exceptions return a descriptive error for diagnostics.",
                $"Module: {ModuleName}",
                "Preconditions: Valid credentials exist and the Google client throws an unexpected exception.",
                "Test Data: InvalidOperationException with message 'boom'.",
                "Test Steps:",
                "1. Persist a valid google-credentials.json.",
                "2. Configure the fake Google client to throw InvalidOperationException.",
                "3. Execute RunImportAsync().",
                "Expected Result: Outcome equals Error and ErrorMessage matches the thrown exception message.");

            await WriteValidCredentialsAsync();

            _calendarClient.ExceptionToThrow = new InvalidOperationException("boom");

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Error, result.Outcome);
            Assert.AreEqual("boom", result.ErrorMessage);
        }

        private static void DocumentTestCase(string id, string title, params string[] details)
        {
            TestContext.WriteLine($"Test Case ID: {id}");
            TestContext.WriteLine($"Title: {title}");

            foreach (var detail in details)
            {
                TestContext.WriteLine(detail);
            }

            TestContext.WriteLine(string.Empty);
        }

        private async Task WriteValidCredentialsAsync()
        {
            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";
            File.WriteAllText(credentialPath, json);

            await _service.ImportCredentialsAsync(credentialPath);
        }

        private sealed class FakeCalendarClient : IGoogleCalendarClient
        {
            public IList<Event> EventsToReturn { get; set; } = new List<Event>();
            public Exception ExceptionToThrow { get; set; }
            public bool WasInvoked { get; private set; }

            public Task<IList<Event>> FetchEventsAsync(Stream credentialStream, string tokenDirectory, DateTime timeMin, DateTime timeMax, CancellationToken cancellationToken)
            {
                WasInvoked = true;

                if (ExceptionToThrow != null)
                {
                    throw ExceptionToThrow;
                }

                return Task.FromResult(EventsToReturn);
            }
        }
    }
}
