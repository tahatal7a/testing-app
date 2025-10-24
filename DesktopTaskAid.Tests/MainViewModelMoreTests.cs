using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using DesktopTaskAid.Models;
using DesktopTaskAid.ViewModels;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainViewModelMoreTests
    {
        [SetUp]
        public void Setup()
        {
            if (Application.Current == null)
            {
                new Application();
            }
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources["IsDarkTheme"] = false;
        }

        [Test]
        public void OpenAndCloseImportModal_And_NavigationCommands()
        {
            var vm = new MainViewModel();

            Assert.IsFalse(vm.IsCalendarImportModalOpen);
            vm.OpenCalendarImportModalCommand.Execute(null);
            Assert.IsTrue(vm.IsCalendarImportModalOpen);
            vm.CloseCalendarImportModalCommand.Execute(null);
            Assert.IsFalse(vm.IsCalendarImportModalOpen);

            var original = vm.CurrentMonth;
            vm.NextMonthCommand.Execute(null);
            Assert.AreEqual(original.AddMonths(1).Month, vm.CurrentMonth.Month);
            vm.PreviousMonthCommand.Execute(null);
            Assert.AreEqual(original.Month, vm.CurrentMonth.Month);

            var newDate = DateTime.Today.AddDays(2);
            vm.SelectDateCommand.Execute(newDate);
            Assert.AreEqual(newDate.Date, vm.SelectedDate.Date);
        }

        [Test]
        public void SaveTask_Validation_WhenNameMissing_DoesNotCloseModal()
        {
            var vm = new MainViewModel();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "   ";
            vm.SaveTaskCommand.Execute(null);

            Assert.IsTrue(vm.IsModalOpen, "Modal should remain open due to validation message");
        }

        [Test]
        public void NextPageCommand_CanExecute_TogglesByTotalPages()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 3; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = "T" + i, DueDate = DateTime.Today.AddDays(i) });
            }

            vm.PageSize = 5; // 1 page only
            Assert.IsFalse(vm.NextPageCommand.CanExecute(null));

            vm.PageSize = 2; // 2 pages
            vm.CurrentPage = 1;
            Assert.IsTrue(vm.NextPageCommand.CanExecute(null));
            vm.CurrentPage = 2;
            Assert.IsFalse(vm.NextPageCommand.CanExecute(null));
        }
    }
}
