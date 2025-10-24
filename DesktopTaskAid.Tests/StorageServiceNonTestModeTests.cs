using System;
using System.IO;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class StorageServiceNonTestModeTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "AppData_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { }
        }

        [Test]
        public void OverloadConstructor_CreatesDirectory_WhenEnsureTrue_AndMissing()
        {
            var custom = Path.Combine(_root, "customFolderA");
            if (Directory.Exists(custom)) Directory.Delete(custom, true);

            var service = new StorageService(custom, ensureDirectoryExists: true);
            Assert.AreEqual(custom, service.GetDataFolderPath());
            Assert.IsTrue(Directory.Exists(custom));
        }

        [Test]
        public void OverloadConstructor_DoesNotCreate_WhenEnsureFalse()
        {
            var custom = Path.Combine(_root, "customFolderB");
            if (Directory.Exists(custom)) Directory.Delete(custom, true);

            var service = new StorageService(custom, ensureDirectoryExists: false);
            Assert.AreEqual(custom, service.GetDataFolderPath());
            Assert.IsFalse(Directory.Exists(custom));
        }

        [Test]
        public void SaveState_And_LoadState_RoundTrip_UsingOverload()
        {
            var custom = Path.Combine(_root, "persistFolder");
            var service = new StorageService(custom, ensureDirectoryExists: true);

            var state = new AppState();
            state.Tasks.Add(new TaskItem { Name = "PersistMe" });
            state.CurrentPage = 3;
            state.PageSize = 20;

            service.SaveState(state);

            var reloaded = service.LoadState();
            Assert.AreEqual(1, reloaded.Tasks.Count);
            Assert.AreEqual("PersistMe", reloaded.Tasks[0].Name);
            Assert.AreEqual(3, reloaded.CurrentPage);
            Assert.AreEqual(20, reloaded.PageSize);
        }
    }
}
