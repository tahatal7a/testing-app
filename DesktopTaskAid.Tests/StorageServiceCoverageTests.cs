using System;
using System.IO;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class StorageServiceCoverageTests
    {
        private string _dir;

        [SetUp]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "SSCov_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void Teardown()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch { }
        }

        [Test]
        public void LoadState_WithNullJson_ReturnsDefault()
        {
            var path = Path.Combine(_dir, "appState.json");
            File.WriteAllText(path, "null");

            var service = new StorageService(_dir);
            var state = service.LoadState();

            Assert.IsNotNull(state);
            Assert.IsNotNull(state.Tasks);
            Assert.AreEqual(0, state.Tasks.Count);
        }

        [Test]
        public void LoadState_WithTasksNull_InitializesEmptyList()
        {
            var json = JsonConvert.SerializeObject(new
            {
                Tasks = (object)null,
                Settings = new AppSettings { Theme = "light", HelperEnabled = false },
                Calendar = new CalendarState { CurrentMonth = DateTime.Today, SelectedDate = DateTime.Today },
                Timer = new TimerState()
            });
            File.WriteAllText(Path.Combine(_dir, "appState.json"), json);

            var service = new StorageService(_dir);
            var state = service.LoadState();

            Assert.IsNotNull(state.Tasks);
            Assert.AreEqual(0, state.Tasks.Count);
        }

        [Test]
        public void LoadState_RefreshDailyTracking_ResetsYesterday()
        {
            var timer = new TimerState
            {
                DoneTodayDate = DateTime.Today.AddDays(-1),
                DoneTodaySeconds = 100
            };
            var toPersist = new AppState
            {
                Tasks = new System.Collections.Generic.List<TaskItem>(),
                Settings = new AppSettings(),
                Calendar = new CalendarState(),
                Timer = timer
            };
            var path = Path.Combine(_dir, "appState.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(toPersist));

            var service = new StorageService(_dir);
            var loaded = service.LoadState();

            Assert.AreEqual(DateTime.Today, loaded.Timer.DoneTodayDate);
            Assert.AreEqual(0, loaded.Timer.DoneTodaySeconds);
        }

        [Test]
        public void SaveState_MissingDirectory_LogsError()
        {
            // Create under a dir we will delete to force DirectoryNotFound on save
            var parent = Path.Combine(_dir, "gone");
            var missing = Path.Combine(parent, "child");
            Directory.CreateDirectory(parent);
            Directory.Delete(parent, true);

            var service = new StorageService(missing, ensureDirectoryExists: false);
            service.SaveState(new AppState());

            var log = File.ReadAllText(LoggingService.GetLogFilePath());
            StringAssert.Contains("ERROR saving state", log);
        }
    }
}
