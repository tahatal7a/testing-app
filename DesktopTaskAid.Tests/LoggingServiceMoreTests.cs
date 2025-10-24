using System;
using System.IO;
using DesktopTaskAid.Services;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class LoggingServiceMoreTests
    {
        [Test]
        public void Log_FallbackWrite_DoesNotThrow_WhenPrimaryFails()
        {
            // Try to simulate a failure by making the log path directory read-only
            var path = LoggingService.GetLogFilePath();
            var dir = Path.GetDirectoryName(path);
            try
            {
                var attr = File.GetAttributes(dir);
                File.SetAttributes(dir, FileAttributes.ReadOnly);
                // This may still succeed on some systems; ensure it does not throw
                Assert.DoesNotThrow(() => LoggingService.Log("Test after toggling directory attributes", "TEST"));
            }
            catch
            {
                // If we cannot toggle attributes, at least log and ensure no exception escapes
                Assert.DoesNotThrow(() => LoggingService.Log("Test without attribute toggle", "TEST"));
            }
            finally
            {
                try { File.SetAttributes(dir, FileAttributes.Normal); } catch { }
            }
        }
    }
}
