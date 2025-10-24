using System;
using System.IO;
using System.Linq;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class StorageServiceMoreTests
    {
        [Test]
        public void DefaultConstructor_InTestMode_DoesNotPersist()
        {
            // Arrange: create a service using default ctor (detects test mode via loaded test assemblies)
            var service = new StorageService();
            var state = service.LoadState();
            state.Tasks.Add(new TaskItem { Name = "A" });

            // Act
            service.SaveState(state);

            // Assert: since test mode skips disk persistence, state file should not exist in test data folder
            var path = Path.Combine(service.GetDataFolderPath(), "appState.json");
            Assert.IsFalse(File.Exists(path));
        }
    }
}
