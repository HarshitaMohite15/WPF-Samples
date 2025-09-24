using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;

namespace ImageView.Tests
{
    public class Tests
    {
        private Process _appProcess;
        private AutomationElement _mainWindow;

        [SetUp]
        public void SetUp()
        {
            var exePath = Path.GetFullPath(@"ImageView.exe");
            Assert.That(File.Exists(exePath), $"Executable not found at: {exePath}");
            _appProcess = Process.Start(exePath);
            Thread.Sleep(500); // Wait for the application to start           
            for (int i = 0; i < 20; i++)
            {
                _mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, "MainWindow"));
                if (_mainWindow != null)
                    break;
                Thread.Sleep(500);
            }
            Assert.That(_mainWindow, Is.Not.Null, "Main window not found.");
        
        }

        [TearDown]
        public void TearDown()
        {
            if (!_appProcess.HasExited)
                _appProcess.Kill();
            _appProcess.Dispose();
        }
        [Test]
        public void ClickImage_LoadsImageInDockPanel()
        {
            // Find the ListBox by AutomationId or Name
            var listBox = _mainWindow.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "imageListBox"));

            Assert.That(listBox, Is.Not.Null, "ListBox not found.");

            // Find the Image control before selection
            var imageControlBefore = _mainWindow.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "currentImage"));
            Assert.That(imageControlBefore, Is.Not.Null, "Image control not found.");
            var imageSourceNameBefore = imageControlBefore.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;
            Assert.That(imageSourceNameBefore, Is.Null.Or.Empty, "Image source is set");

            // Get the first ListBoxItem
            var listItem = listBox.FindFirst(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));
            Assert.That(listItem, Is.Not.Null, "No ListBoxItem found.");

            var listItemName = listItem.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;
            Assert.That(listItemName, Is.Not.Null.And.Not.Empty, "List item name is empty.");
            // Select the item
            var selectPattern = listItem.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            Assert.That(selectPattern, Is.Not.Null, "SelectionItemPattern not supported.");
            selectPattern.Select();

            Thread.Sleep(500); // Wait for UI to update
            // Find the Image control
            var imageControl = _mainWindow.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "currentImage"));
            Assert.That(imageControl, Is.Not.Null, "Image control not found.");

            var imageSourceName = imageControl.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;
            Assert.That(imageSourceName, Is.Not.Null.And.Not.Empty, "Image source not set");

            Assert.That(imageSourceName, Is.EqualTo(listItemName), "Image source does not match selected item.");

        }
    }
    }
