using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3.Data;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class CalendarImportServiceTests
    {
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
        public async Task RunImportAsync_ReturnsMissingCredentials_WhenFileAbsent()
        {
            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.MissingCredentials, result.Outcome);
            Assert.IsFalse(_calendarClient.WasInvoked);
        }

        [Test]
        public async Task RunImportAsync_ReturnsInvalidCredentials_WhenJsonMalformed()
        {
            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            File.WriteAllText(credentialPath, "not json");

            await _service.ImportCredentialsAsync(credentialPath);

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.InvalidCredentials, result.Outcome);
            Assert.IsFalse(_calendarClient.WasInvoked);
        }

        [Test]
        public async Task RunImportAsync_ReturnsCancelled_WhenClientReturnsNull()
        {
            await WriteValidCredentialsAsync();
            _calendarClient.EventsToReturn = null;

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Cancelled, result.Outcome);
        }

        [Test]
        public async Task RunImportAsync_ReturnsNoEvents_WhenClientReturnsEmptyList()
        {
            await WriteValidCredentialsAsync();
            _calendarClient.EventsToReturn = new List<Event>();

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.NoEvents, result.Outcome);
        }

        [Test]
        public async Task RunImportAsync_ReturnsSuccess_WithConvertedTasks()
        {
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
        public async Task RunImportAsync_ReturnsAccessBlocked_WhenForbidden()
        {
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
        public async Task RunImportAsync_ReturnsCancelled_WhenTokenAccessDenied()
        {
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
        public async Task RunImportAsync_ReturnsError_ForUnexpectedException()
        {
            await WriteValidCredentialsAsync();

            _calendarClient.ExceptionToThrow = new InvalidOperationException("boom");

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Error, result.Outcome);
            Assert.AreEqual("boom", result.ErrorMessage);
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
