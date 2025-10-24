using System;
using System.Threading;
using System.Windows.Input;
using DesktopTaskAid.Helpers;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class RelayCommandTests
    {
        [Test]
        public void Constructor_NullExecute_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new RelayCommand(null));
        }

        [Test]
        public void CanExecute_DefaultsToTrue_WhenNoPredicate()
        {
            var cmd = new RelayCommand(_ => { });
            Assert.IsTrue(cmd.CanExecute(null));
        }

        [Test]
        public void CanExecute_RespectsPredicate()
        {
            var allow = false;
            var cmd = new RelayCommand(_ => { }, _ => allow);

            Assert.IsFalse(cmd.CanExecute(null));
            allow = true;
            Assert.IsTrue(cmd.CanExecute(null));
        }

        [Test]
        public void Execute_InvokesAction()
        {
            object received = null;
            var cmd = new RelayCommand(p => received = p);
            var marker = new object();

            cmd.Execute(marker);
            Assert.AreSame(marker, received);
        }

        [Test]
        public void RaiseCanExecuteChanged_IsCallable()
        {
            var cmd = new RelayCommand(_ => { });
            // We cannot reliably assert event count due to CommandManager aggregation in WPF.
            // This call should not throw.
            Assert.DoesNotThrow(() => cmd.RaiseCanExecuteChanged());
        }
    }
}
