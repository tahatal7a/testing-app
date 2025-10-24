using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using DesktopTaskAid.Converters;
using DesktopTaskAid.ViewModels;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ConverterTests
    {
        [SetUp]
        public void Setup()
        {
            if (Application.Current == null)
            {
                new Application();
            }

            Application.Current.Resources.MergedDictionaries.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Application.Current.Resources.MergedDictionaries.Clear();
        }

        [Test]
        public void BoolToVisibilityConverter_BasicAndInverse()
        {
            var converter = new BoolToVisibilityConverter();

            Assert.AreEqual(Visibility.Visible, converter.Convert(true, typeof(Visibility), null, null));
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(false, typeof(Visibility), null, null));
            Assert.AreEqual(Visibility.Collapsed, converter.Convert("not bool", typeof(Visibility), null, null));
            Assert.AreEqual(Visibility.Collapsed, converter.Convert(true, typeof(Visibility), "Inverse", null));
            Assert.AreEqual(Visibility.Visible, converter.Convert(false, typeof(Visibility), "Inverse", null));
            Assert.IsTrue((bool)converter.ConvertBack(Visibility.Visible, typeof(bool), null, null));
            Assert.IsFalse((bool)converter.ConvertBack(Visibility.Collapsed, typeof(bool), null, null));
        }

        [Test]
        public void CalendarDayTagConverter_ReturnsRequestedProperty()
        {
            var converter = new CalendarDayTagConverter();
            var day = new CalendarDay { IsSelected = true, IsToday = false };

            Assert.IsTrue((bool)converter.Convert(day, typeof(bool), "IsSelected", null));
            Assert.IsFalse((bool)converter.Convert(day, typeof(bool), "IsToday", null));
            Assert.IsFalse((bool)converter.Convert("not day", typeof(bool), "IsToday", null));
            Assert.Throws<NotImplementedException>(() => converter.ConvertBack(true, typeof(CalendarDay), null, null));
        }

        [Test]
        public void DateFormatConverter_FormatsAndParses()
        {
            var converter = new DateFormatConverter();
            var date = new DateTime(2024, 5, 10);

            Assert.AreEqual("10-05-2024", converter.Convert(date, typeof(string), null, null));
            var parsed = converter.ConvertBack("10-05-2024", typeof(DateTime), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(date, parsed);
            Assert.AreEqual(string.Empty, converter.Convert("not date", typeof(string), null, null));
            Assert.IsNull(converter.ConvertBack("bad", typeof(DateTime), null, CultureInfo.InvariantCulture));
        }

        //[Test]
        //public void IconStrokeConverter_UsesThemeIndicator()
        //{
        //    var converter = new IconStrokeConverter();

        //    var darkDictionary = new ResourceDictionary
        //    {
        //        Source = new Uri("pack://application:,,,/Themes/darkTheme.xaml", UriKind.Absolute)
        //    };
        //    Application.Current.Resources.MergedDictionaries.Add(darkDictionary);

        //    var darkBrush = (Brush)converter.Convert(null, typeof(Brush), null, null);
        //    Assert.AreEqual(Brushes.White, darkBrush);

        //    Application.Current.Resources.MergedDictionaries.Clear();
        //    Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary());

        //    var lightBrush = (SolidColorBrush)converter.Convert(null, typeof(Brush), null, null);
        //    Assert.AreEqual(Color.FromRgb(0x1A, 0x1A, 0x1A), lightBrush.Color);
        //}

        [Test]
        public void ReminderStatusToBrushConverter_ReturnsExpectedColors()
        {
            var converter = new ReminderStatusToBrushConverter();

            var active = (SolidColorBrush)converter.Convert("active", typeof(Brush), null, null);
            var overdue = (SolidColorBrush)converter.Convert("overdue", typeof(Brush), null, null);
            var none = (SolidColorBrush)converter.Convert("none", typeof(Brush), null, null);
            var fallback = (SolidColorBrush)converter.Convert(123, typeof(Brush), null, null);

            Assert.AreEqual(Color.FromRgb(180, 223, 210), active.Color);
            Assert.AreEqual(Color.FromRgb(255, 194, 181), overdue.Color);
            Assert.AreEqual(Color.FromRgb(255, 238, 181), none.Color);
            Assert.AreEqual(Color.FromRgb(255, 238, 181), fallback.Color);
        }

        [Test]
        public void ReminderStatusToTextColorConverter_ReturnsExpectedColors()
        {
            var converter = new ReminderStatusToTextColorConverter();

            var active = (SolidColorBrush)converter.Convert("active", typeof(Brush), null, null);
            var overdue = (SolidColorBrush)converter.Convert("overdue", typeof(Brush), null, null);
            var none = (SolidColorBrush)converter.Convert("none", typeof(Brush), null, null);
            var fallback = (SolidColorBrush)converter.Convert(123, typeof(Brush), null, null);

            Assert.AreEqual(Color.FromRgb(24, 119, 91), active.Color);
            Assert.AreEqual(Color.FromRgb(212, 48, 41), overdue.Color);
            Assert.AreEqual(Color.FromRgb(212, 152, 41), none.Color);
            Assert.AreEqual(Color.FromRgb(212, 152, 41), fallback.Color);
        }

        [Test]
        public void SecondsToDurationConverter_FormatsValues()
        {
            var converter = new SecondsToDurationConverter();
            Assert.AreEqual("1:01:01", converter.Convert(3661, typeof(string), null, null));
            Assert.AreEqual("0:00:00", converter.Convert("bad", typeof(string), null, null));
        }

        [Test]
        public void SecondsToTimerDisplayConverter_FormatsValues()
        {
            var converter = new SecondsToTimerDisplayConverter();
            Assert.AreEqual("05:10", converter.Convert(310, typeof(string), null, null));
            Assert.AreEqual("00:00", converter.Convert("bad", typeof(string), null, null));
        }

        [Test]
        public void TimeFormatConverter_FormatsTimes()
        {
            var converter = new TimeFormatConverter();
            Assert.AreEqual("1 PM", converter.Convert(TimeSpan.FromHours(13), typeof(string), null, null));
            Assert.AreEqual("2:30 AM", converter.Convert(new TimeSpan(2, 30, 0), typeof(string), null, null));
            Assert.AreEqual(string.Empty, converter.Convert("bad", typeof(string), null, null));
        }
    }
}
