using System;
using System.Threading;
using System.Windows.Input;
using DesktopTaskAid.Helpers;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class HelperTests
    {
        [Test]
        public void RelayCommand_ExecutesAction()
        {
            object received = null;
            var command = new RelayCommand(p => received = p, p => p is int i && i > 0);

            Assert.IsFalse(command.CanExecute(0));
            Assert.IsTrue(command.CanExecute(1));

            command.Execute(5);
            Assert.AreEqual(5, received);

            // Should not throw when raising CanExecuteChanged
            command.RaiseCanExecuteChanged();
        }

        [Test]
        public void ViewModelBase_SetPropertyRaisesEvents()
        {
            var vm = new SampleViewModel();
            string property = null;
            vm.PropertyChanged += (_, args) => property = args.PropertyName;

            Assert.IsTrue(vm.SetValue("hello"));
            Assert.AreEqual("Value", property);

            property = null;
            Assert.IsFalse(vm.SetValue("hello"));
            Assert.IsNull(property);

            vm.TriggerOnPropertyChanged("Other");
            Assert.AreEqual("Other", property);
        }

        private sealed class SampleViewModel : ViewModelBase
        {
            private string _value;

            public string Value
            {
                get => _value;
                set => SetProperty(ref _value, value);
            }

            public bool SetValue(string value)
            {
                return SetProperty(ref _value, value, nameof(Value));
            }

            public void TriggerOnPropertyChanged(string name)
            {
                OnPropertyChanged(name);
            }
        }
    }
}
