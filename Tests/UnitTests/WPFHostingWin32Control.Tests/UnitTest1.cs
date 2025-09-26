using System.Diagnostics;
using System.Windows.Automation;

namespace WPFHostingWin32Control.Tests
{
    public class Tests
    {
        private Process _process;
        private AutomationElement _mainWindow;

        [SetUp]
        public void Setup()
        {
            var exePath = Path.GetFullPath(@"WPFHostingWin32Control.exe");
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
            Assert.That(_mainWindow, Is.Not.Null, "Main window not found.");
        }

        [TearDown]
        public void TearDown()
        {
            if (!_process.HasExited)
                _process.Kill();
            _process.Dispose();
        }

        [Test]
        public void AppendDeleteText_AppendsItemToList_IncreasesItemCount()
        {
            // Find controls
            var txtAppend = _mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "txtAppend"));
            var appendButton = _mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, "Append"));
            var numItems = _mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "numItems"));

            Assert.That(txtAppend, Is.Not.Null, "txtAppend not found.");
            Assert.That(appendButton, Is.Not.Null, "Append button not found.");
            Assert.That(numItems, Is.Not.Null, "numItems not found.");

            // Get initial count
            var numItemsValue = numItems.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;
            Assert.That(numItemsValue, Is.Not.Null, "numItems value is null.");
            int initialCount = int.Parse(numItemsValue);

            // Set text
            var valuePattern = txtAppend.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
            Assert.That(valuePattern, Is.Not.Null, "ValuePattern not supported on txtAppend.");
            valuePattern.SetValue("NewItem");
            // Click append
            var invokePattern = appendButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            Assert.That(invokePattern, Is.Not.Null, "InvokePattern not supported on Append button.");
            invokePattern.Invoke();

            // Wait for UI to update
            Thread.Sleep(500);

            // Get new count
            numItemsValue = numItems.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;
            Assert.That(numItemsValue, Is.Not.Null, "numItems value is null.");
            int newCount = int.Parse(numItemsValue);

            Assert.That(newCount, Is.EqualTo(initialCount + 1), "Item count should increase by 1 after append.");

            //Delete the item
            var listBox = _mainWindow.FindFirst(TreeScope.Descendants,
                            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.List));

            Assert.That(listBox, Is.Not.Null, "ListBox not found.");

            // Find the first item in the ListBox
            var listItem = listBox.FindFirst(TreeScope.Children,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));

            Assert.That(listItem, Is.Not.Null, "ListBox item not found.");

            // Select the item using SelectionItemPattern
            var selectionItemPattern = listItem.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            Assert.That(selectionItemPattern, Is.Not.Null, "SelectionItemPattern not supported on ListBox item.");

            selectionItemPattern.Select();
            var deleteButton = _mainWindow.FindFirst(TreeScope.Descendants,
               new PropertyCondition(AutomationElement.NameProperty, "Delete"));

            Assert.That(numItems, Is.Not.Null, "numItems not found.");
            Assert.That(deleteButton, Is.Not.Null, "Delete button not found.");
            var invokeDeletePattern = deleteButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            Assert.That(invokeDeletePattern, Is.Not.Null, "InvokePattern not supported on Delete button.");
            invokeDeletePattern.Invoke();

            // Wait for UI to update
            Thread.Sleep(1000);

            // Get new count
            numItemsValue = numItems.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;
            Assert.That(numItemsValue, Is.Not.Null, "numItems value is null.");
            int newDeleteCount = int.Parse(numItemsValue);

            Assert.That(newDeleteCount, Is.EqualTo(newCount - 1), "Item count should decrease by 1 after delete.");
        }


        
    }
}
