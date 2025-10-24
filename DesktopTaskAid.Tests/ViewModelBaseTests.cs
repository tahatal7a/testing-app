using System.ComponentModel;
using System.Threading;
using DesktopTaskAid.Helpers;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ViewModelBaseTests
    {
        private sealed class DummyVm : ViewModelBase
        {
            private int _x;
            public int X
            {
                get => _x;
                set => SetProperty(ref _x, value);
            }
        }

        [Test]
        public void SetProperty_RaisesPropertyChanged_WhenValueChanges()
        {
            var vm = new DummyVm();
            string lastProp = null;
            vm.PropertyChanged += (s, e) => lastProp = e.PropertyName;

            vm.X = 5;

            Assert.AreEqual("X", lastProp);
        }

        [Test]
        public void SetProperty_NoRaise_WhenValueSame()
        {
            var vm = new DummyVm();
            int count = 0;
            vm.PropertyChanged += (s, e) => count++;

            vm.X = 1;
            vm.X = 1; // no change

            Assert.AreEqual(1, count);
        }
    }
}
