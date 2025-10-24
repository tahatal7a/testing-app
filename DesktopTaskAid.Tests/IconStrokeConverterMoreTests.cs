using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using DesktopTaskAid.Converters;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class IconStrokeConverterMoreTests
    {
        [SetUp]
        public void Setup()
        {
            if (Application.Current == null)
            {
                new Application();
            }
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.Remove("IsDarkTheme");
            Application.Current.Resources.Remove("ThemeName");
        }

        [Test]
        public void Convert_UsesThemeNameFallback_WhenProvided()
        {
            var conv = new IconStrokeConverter();
            Application.Current.Resources["ThemeName"] = "dark";

            var brush = (SolidColorBrush)conv.Convert(null, typeof(Brush), null, null);
            Assert.AreEqual(Brushes.White.Color, brush.Color);
        }

        [Test]
        public void Convert_DefaultsToLightBrush_WhenNoFlagsOrDictionaries()
        {
            var conv = new IconStrokeConverter();
            // No IsDarkTheme, no ThemeName, no merged dictionaries with dark in name
            var brush = (SolidColorBrush)conv.Convert(null, typeof(Brush), null, null);
            Assert.AreEqual(Color.FromRgb(0x1A, 0x1A, 0x1A), brush.Color);
        }

        [Test]
        public void ConvertBack_Throws_NotImplemented()
        {
            var conv = new IconStrokeConverter();
            Assert.Throws<NotImplementedException>(() => conv.ConvertBack(null, null, null, null));
        }
    }
}
