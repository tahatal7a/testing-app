using System;
using System.IO;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class StorageServicePersistenceTests
    {
        private string _dir;
        private StorageService _service;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _service = new StorageService(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            }
            catch { }
        }

        [Test]
        public void SaveState_CreatesFolder_And_File()
        {
            var state = new AppState();
            _service.SaveState(state);
            Assert.IsTrue(File.Exists(Path.Combine(_dir, "appState.json")));
        }

        [Test]
        public void LoadState_AfterSave_RestoresValues()
        {
            var state = new AppState();
            state.CurrentPage = 3;
            state.PageSize = 7;
            state.Tasks.Add(new TaskItem { Name = "Persisted" });

            _service.SaveState(state);

            var reloaded = _service.LoadState();
            Assert.AreEqual(3, reloaded.CurrentPage);
            Assert.AreEqual(7, reloaded.PageSize);
            Assert.AreEqual(1, reloaded.Tasks.Count);
            Assert.AreEqual("Persisted", reloaded.Tasks[0].Name);
        }
    }
}
