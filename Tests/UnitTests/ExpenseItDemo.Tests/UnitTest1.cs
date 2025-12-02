using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace ExpenseItDemo.Tests
{
    public class Tests
    {
        private Process _appProcess;
        private AutomationElement _mainWindow;

        [SetUp]
        public void SetUp()
        {               
            var exePath = Path.GetFullPath(@"ExpenseIt9.exe");
            Assert.That(File.Exists(exePath), $"Executable not found at: {exePath}");
            _appProcess = Process.Start(exePath);

            // Wait for main window to be ready
            for (int i = 0; i < 20; i++)
            {
                _mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, "ExpenseIt Standalone"));
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
        public void CostCenterComboBox_Should_Expand_And_Select_First_Item()
        {
            // Find the ComboBox by its AutomationId or Name
            var comboBox = _mainWindow.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "costCenterTextBox"));

            Assert.That(comboBox, Is.Not.Null, "Cost Center ComboBox not found.");

            // Expand the ComboBox
            var expandCollapsePattern = comboBox.GetCurrentPattern(ExpandCollapsePattern.Pattern) as ExpandCollapsePattern;
            Assert.That(expandCollapsePattern, Is.Not.Null, "ExpandCollapsePattern not supported.");
            expandCollapsePattern.Expand();

            // Wait for items to appear
            Thread.Sleep(500);

            // Find the first item in the ComboBox
            var listItem = comboBox.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));

            Assert.That(listItem, Is.Not.Null, "No items found in Cost Center ComboBox.");

            // Select the first item
            var selectionItemPattern = listItem.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            Assert.That(selectionItemPattern, Is.Not.Null, "SelectionItemPattern not supported.");
            selectionItemPattern.Select();

            // Optionally, verify selection
            Assert.That(selectionItemPattern.Current.IsSelected, Is.True, "First item was not selected.");
        }

        [Test]
        public void EmailTextBox_Should_Accept_Input()
        {
            // Find the Email TextBox by its AutomationId
            var emailTextBox = _mainWindow.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "emailTextBox"));

            Assert.That(emailTextBox, Is.Not.Null, "Email TextBox not found.");

            // Set the value using ValuePattern
            var valuePattern = emailTextBox.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
            Assert.That(valuePattern, Is.Not.Null, "ValuePattern not supported.");

            string testEmail = "test@example.com";
            valuePattern.SetValue(testEmail);

            // Optionally, verify the value
            Assert.That(valuePattern.Current.Value, Is.EqualTo(testEmail), "Email TextBox value was not set correctly.");
        }
        [Test]
        public void TestClickFileMenuCreateExpenseReport()
        {
            // Find the 'Menu' element in the DockPanel (we can find it by Name or ControlType)
            var menu = FindMenuByName("Expense It Demo");
            Assert.That(menu, Is.Not.Null, "Menu not found.");

            // Find the 'File' menu item inside the Menu
            var fileMenuItem = FindMenuItemByHeader(menu, "File Menu");
            Assert.That(fileMenuItem, Is.Not.Null, "'File' menu item not found.");
            // Expand the 'File' menu if necessary
            ExpandMenuItem(fileMenuItem);
            // Find the 'Create Expense Report' menu item inside the 'File' menu item
            var createExpenseReportMenuItem = FindMenuItemByHeader(fileMenuItem, "Create Expense Report");
            Assert.That(createExpenseReportMenuItem, Is.Not.Null, "'Create Expense Report' menu item not found.");

            // Get the InvokePattern on the Create Expense Report menu item
            var createExpenseReportMenuItemInvokePattern = createExpenseReportMenuItem.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            Assert.That(createExpenseReportMenuItemInvokePattern, Is.Not.Null, "'InvokePattern' is not supported on the 'Create Expense Report' menu item.");
            var automationThread = new Thread(() =>
            {
                // Wait for the dialog to appear
                Thread.Sleep(1000);

                // Find the FontDialogBox window
                var desktop = AutomationElement.RootElement;
                AutomationElement createExpenseReportWindow = null;
                for (int i = 0; i < 10 && createExpenseReportWindow == null; i++)
                {
                    createExpenseReportWindow = desktop.FindFirst(
                        TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Create Expense Report")
                    );
                    if (createExpenseReportWindow == null) Thread.Sleep(200);
                }
                Assert.That(createExpenseReportWindow, Is.Not.Null, "Font dialog not found");        
            });
            automationThread.Start();

            // Invoke the Create Expense Report menu item (simulates a click)
            createExpenseReportMenuItemInvokePattern.Invoke();

            Thread.Sleep(1000);  // Simulate a small wait time for the UI to respond

            automationThread.Join();           
        }
        [Test]
        public void TestClickFileMenuExit()
        {
            // Find the 'Menu' element in the DockPanel (we can find it by Name or ControlType)
            var menu = FindMenuByName("Expense It Demo");
            Assert.That(menu, Is.Not.Null, "Menu not found.");

            // Find the 'File' menu item inside the Menu
            var fileMenuItem = FindMenuItemByHeader(menu, "File Menu");
            Assert.That(fileMenuItem, Is.Not.Null, "'File' menu item not found.");
            // Expand the 'File' menu if necessary
            ExpandMenuItem(fileMenuItem);
            // Find the 'Exit' menu item inside the 'File' menu item
            var exitMenuItem = FindMenuItemByHeader(fileMenuItem, "Exit");
            Assert.That(exitMenuItem, Is.Not.Null, "'Exit' menu item not found.");

            // Get the InvokePattern on the Exit menu item
            var exitMenuItemInvokePattern = exitMenuItem.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            Assert.That(exitMenuItemInvokePattern, Is.Not.Null, "'InvokePattern' is not supported on the 'Exit' menu item.");

            // Invoke the Exit menu item (simulates a click)
            exitMenuItemInvokePattern.Invoke();

            Thread.Sleep(500);  // Simulate a small wait time for the UI to respond
             //  Validate that the exit action was triggered
            var windowAfterExit = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, "ExpenseIt Standalone"));
            Assert.That(windowAfterExit, Is.Null,"The application window was not closed after clicking Exit.");
        }

        private AutomationElement FindMenuByName(string name)
        {
            // Search for the Menu by its name
            var condition = new PropertyCondition(AutomationElement.NameProperty, name);
            return _mainWindow.FindFirst(TreeScope.Descendants, condition);
        }

        private AutomationElement FindMenuItemByHeader(AutomationElement parentMenu, string header)
        {
            // Search for a MenuItem by its header within a specific menu
            var condition = new PropertyCondition(AutomationElement.NameProperty, header);
            return parentMenu.FindFirst(TreeScope.Descendants, condition);
        }
        private void ExpandMenuItem(AutomationElement menuItem)
        {
           
            var expandCollapsePattern = menuItem.GetCurrentPattern(ExpandCollapsePattern.Pattern) as ExpandCollapsePattern;
            if (expandCollapsePattern != null)
            {
                expandCollapsePattern.Expand();
            }
        }

    }
}
