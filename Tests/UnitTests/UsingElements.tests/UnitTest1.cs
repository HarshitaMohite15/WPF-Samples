using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace UsingElements.tests
{
    public class Tests
    {
        private Process _process;
        private AutomationElement _mainWindow;

        [SetUp]
        public void SetUp()
        {
            var exePath = Path.GetFullPath(@"UsingElements.exe");
            _process = Process.Start(exePath);

            // Wait for main window to be ready
            int retries = 0;
            while (_mainWindow == null && retries < 20)
            {
                Thread.Sleep(500);
                _mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, "MainWindow"));
                retries++;
            }
           
            Assert.That(_mainWindow, Is.Not.Null, "MainWindow not found.");
        }

        [TearDown]
        public void TearDown()
        {
            if (!_process.HasExited)
                _process.Kill();
            _process.Dispose();
        }

        [Test]
        public void AddButton_Tab_AddsButtonToStackPanel()
        {
            // Find TabControl
            var tabControl = _mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Tab));

            Assert.That(tabControl, Is.Not.Null, "TabControl not found.");
          
            // Find "Add Control" TabItem
            var tabItems = tabControl.FindAll(TreeScope.Children,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
            AutomationElement addTab = null;
            foreach (AutomationElement tab in tabItems)
            {
                if (tab.Current.Name == "Add Control")
                {
                    addTab = tab;
                    break;
                }
            }
            Assert.That(addTab, Is.Not.Null, "'Add Control' TabItem not found.");
           
            // Select the tab (invoke click)
            var selectPattern = addTab.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
           
            Assert.That(selectPattern, Is.Not.Null, "selectPattern not supported on TabItem.");
            selectPattern.Select();

            Thread.Sleep(500); // Wait for UI update

            // Find Button inside StackPanel
            System.Windows.Automation.Condition condition = new AndCondition(
                                        new PropertyCondition(AutomationElement.NameProperty, "New Button"),
                                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
            var button = _mainWindow.FindFirst(TreeScope.Descendants, condition);

            Assert.That(button, Is.Not.Null, "Button not found in StackPanel after Add Control tab clicked.");
            
        }

    }
    }
