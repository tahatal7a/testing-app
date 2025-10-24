using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DesktopTaskAid.Models;
using DesktopTaskAid.ViewModels;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainViewModelEdgeTests
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
        public void SaveTask_ActiveWithoutDue_SetsGenericActiveLabel()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "No Due";
            vm.EditingTask.DueDate = null;
            vm.EditingTask.DueTime = null;
            vm.EditingTask.ReminderStatus = "active";

            vm.SaveTaskCommand.Execute(null);

            var added = vm.AllTasks.Single(t => t.Name == "No Due");
            Assert.AreEqual("Active", added.ReminderLabel);
        }
    }
}
