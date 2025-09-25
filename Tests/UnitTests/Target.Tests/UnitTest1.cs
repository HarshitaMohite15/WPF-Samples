using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Automation;

namespace Target.Tests
{
    public class Tests
    {
        private Process _appProcess;

        [SetUp]
        public void Setup()
        {
            _appProcess = Process.Start(Path.GetFullPath(@"Target.exe"));
            Assert.That(_appProcess, Is.Not.EqualTo(null), "Failed to launch app");

            // Give it time to load
            Thread.Sleep(1000);
        }

        [TearDown]
        public void Teardown()
        {

            if (!_appProcess.HasExited)
            { _appProcess.Kill(); }

            _appProcess.Dispose();
        }

        [Test]
        public void ExpandEmployee1_ClickJesperButton_ShouldShowMessageBox()
        {
            // Get the root automation element (desktop)
            var desktop = AutomationElement.RootElement;
            AutomationElement mainWindow = null;
            // Find the main window of the application
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(1000);
                mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, "MainWindow")
                );

                if (mainWindow != null) break;
            }
            Assert.That(mainWindow, Is.Not.EqualTo(null), "Main window not found.");
            // Find all TreeViews
            var treeViews = mainWindow.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Tree));

            Assert.That(treeViews.Count, Is.GreaterThanOrEqualTo(2), "Expected at least 2 TreeViews.");

            // Get second TreeView (with buttons in it)
            var treeView = treeViews[1]; // index 1 (2nd one)

            // Find "Employee1" TreeViewItem
            var employee1Node = treeView.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, "Employee1"));

            Assert.That(employee1Node, Is.Not.EqualTo(null), "Employee1 node  not found.");
            // Expand the node if it's collapsible
            var expandCollapse = employee1Node.GetCurrentPattern(ExpandCollapsePattern.Pattern) as ExpandCollapsePattern;
            expandCollapse?.Expand();
            Thread.Sleep(1000); // Let children load
                                // Find the "Jesper" button

            var jesperButton = employee1Node.FindFirst(TreeScope.Descendants,
                                new AndCondition(
                                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                                        new PropertyCondition(AutomationElement.NameProperty, "Jesper")
                                ));

            Assert.That(jesperButton, Is.Not.Null, "Jesper button  not found.");
            // Invoke the button

            var messageCloser = new Thread(() =>
            {
                Thread.Sleep(500); // Wait for MessageBox to appear

                AutomationElement desktop = AutomationElement.RootElement;
                System.Windows.Automation.Condition condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);


                var msgBox = desktop.FindFirst(TreeScope.Children,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

                if (msgBox != null) // Check if the MessageBox title matches the parent window
                {
                    var messageText = msgBox.FindFirst(TreeScope.Descendants,
              new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));

                    //Assert.IsTrue(messageText.Current.Name.Contains("Jesper"), "MessageBox text does not contain 'Jesper'.");
                    Assert.That(messageText.Current.Name, Does.Contain("Jesper"), "MessageBox text does not contain 'Jesper'.");

                    var okButton = msgBox.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "OK"));

                    Assert.That(okButton, Is.Not.Null, "OK button not found.");
                    var invoke = okButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                    invoke?.Invoke();

                }
            });
            messageCloser.Start();
            var invokePattern = jesperButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            invokePattern?.Invoke();

            Thread.Sleep(1000); // Let MessageBox appear
            messageCloser.Join();
        }
    }
}
