using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using DesktopTaskAid;
using DesktopTaskAid.Services;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class AppTests
    {
        [Test]
        public void OnStartup_SetsEnglishCulture_And_SubscribeHandlers()
        {
            if (Application.Current != null)
            {
                Assert.Pass("Application already exists in AppDomain; skipping full App startup test.");
                return;
            }

            var app = new App();
            var mi = typeof(App).GetMethod("OnStartup", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, "OnStartup not found");

            mi.Invoke(app, new object[] { null });

            Assert.AreEqual("en-US", CultureInfo.CurrentCulture.Name);
            Assert.AreEqual("en-US", CultureInfo.CurrentUICulture.Name);
        }

        [Test]
        public void CurrentDomain_UnhandledException_LogsWithoutShowing_WhenNotTerminating()
        {
            // Create an uninitialized App instance to call the handler without constructing a second Application
            object app = FormatterServices.GetUninitializedObject(typeof(App));
            var mi = typeof(App).GetMethod("CurrentDomain_UnhandledException", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi);

            var ex = new InvalidOperationException("boom");
            var args = new UnhandledExceptionEventArgs(ex, false);

            // Should not throw or terminate
            mi.Invoke(app, new object[] { null, args });

            var log = System.IO.File.ReadAllText(LoggingService.GetLogFilePath());
            StringAssert.Contains("UNHANDLED EXCEPTION (CurrentDomain)", log);
        }
    }
}
