using System;
using System.Collections.Generic;
using System.IO;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class StorageServiceTests
    {
        private string _tempDirectory;
        private StorageService _service;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
            _service = new StorageService(_tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Test]
        public void Constructor_WithNullPath_Throws()
        {
            Assert.Throws<ArgumentException>(() => new StorageService(null));
        }

        [Test]
        public void LoadState_WhenFileMissing_ReturnsDefaultState()
        {
            var state = _service.LoadState();

            Assert.IsNotNull(state);
            Assert.IsNotNull(state.Tasks);
            Assert.IsNotNull(state.Settings);
            Assert.AreEqual(0, state.Tasks.Count);
        }

        [Test]
        public void SaveAndLoadState_RoundTripsData()
        {
            var state = new AppState
            {
                Tasks = new List<TaskItem>
                {
                    new TaskItem { Name = "Task One", DueDate = DateTime.Today }
                },
                CurrentPage = 2,
                PageSize = 5
            };

            _service.SaveState(state);

            var reloaded = _service.LoadState();

            Assert.AreEqual(1, reloaded.Tasks.Count);
            Assert.AreEqual("Task One", reloaded.Tasks[0].Name);
            Assert.AreEqual(2, reloaded.CurrentPage);
            Assert.AreEqual(5, reloaded.PageSize);
        }

        [Test]
        public void LoadState_WithInvalidJson_ReturnsDefault()
        {
            File.WriteAllText(Path.Combine(_tempDirectory, "appState.json"), "not json");

            var state = _service.LoadState();

            Assert.IsNotNull(state);
            Assert.AreEqual(0, state.Tasks.Count);
        }

        [Test]
        public void SaveState_WithNullTasks_SerialisesEmptyList()
        {
            var state = new AppState
            {
                Tasks = null
            };

            _service.SaveState(state);

            var json = File.ReadAllText(Path.Combine(_tempDirectory, "appState.json"));
            StringAssert.Contains("\"Tasks\": []", json);
        }

        [Test]
        public void GetDataFolderPath_ReturnsDirectory()
        {
            Assert.AreEqual(_tempDirectory, _service.GetDataFolderPath());
        }
    }
}
