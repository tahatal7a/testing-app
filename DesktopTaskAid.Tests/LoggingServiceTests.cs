using System;
using System.IO;
using DesktopTaskAid.Services;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class LoggingServiceTests
    {
        [Test]
        public void Log_WritesMessageToLogFile()
        {
            var path = LoggingService.GetLogFilePath();
            var marker = Guid.NewGuid().ToString();

            LoggingService.Log($"Test entry {marker}", "TEST");

            Assert.IsTrue(File.Exists(path));
            var text = File.ReadAllText(path);
            StringAssert.Contains(marker, text);
        }

        [Test]
        public void LogError_IncludesExceptionDetails()
        {
            var path = LoggingService.GetLogFilePath();
            var marker = Guid.NewGuid().ToString();
            var exception = new InvalidOperationException(marker);

            LoggingService.LogError("Boom", exception);

            var text = File.ReadAllText(path);
            StringAssert.Contains("InvalidOperationException", text);
            StringAssert.Contains(marker, text);
        }
    }
}
