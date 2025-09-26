using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UsingElements.tests
{
    [Apartment(ApartmentState.STA)]
    public class UnitTest
    {
        private MainWindow _window;
        [SetUp]
        public void Setup()
        {
            _window = new MainWindow();
            _window.Show();
        }

        [TearDown]
        public void TearDown()
        {
            _window.Close();
        }

        [Test]
        public void TestAddButton()
        {
            var addButton = _window.FindName("AddTab") as TabItem;
            Assert.That(addButton, Is.Not.Null, "TabItem not found.");

            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonUpEvent,
                Source = addButton
            };
            addButton.RaiseEvent(args);
            Thread.Sleep(500); // Wait for UI to update
            var sp1 = _window.FindName("sp1") as StackPanel;
            Assert.That(sp1, Is.Not.Null, "StackPanel not found.");
            Assert.That(sp1.FindName("NewButton"), Is.Not.Null, "Button was not added to StackPanel.");
        }

        [Test]
        public void TestGetUIElementCount()
        {
            var countTabItem = _window.FindName("CountTab") as TabItem;
            Assert.That(countTabItem, Is.Not.Null, "TabItem not found.");
            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonUpEvent,
                Source = countTabItem
            };
            countTabItem.RaiseEvent(args);
            Thread.Sleep(500); // Wait for UI to update
            var sp1 = _window.FindName("sp1") as StackPanel;
            Assert.That(sp1, Is.Not.Null, "StackPanel not found.");
            Assert.That(sp1.Children, Has.Count.EqualTo(1), "UIElement count is incorrect.");
        }
    }
}
