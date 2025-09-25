using NUnit.Framework;
using System.Diagnostics;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Documents;
using System.Windows.Forms;

namespace SelectionTarget.Tests
{
    public class Tests
    {
        private Process _process;
        private AutomationElement _mainWindow;
        [SetUp]
        public void SetUp()
        {
            // Start the application
            _process = Process.Start(@"SelectionTarget.exe");
            Thread.Sleep(2000); // Wait for the UI to load
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(1000);
                _mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, "SelectionPatternTarget")); // <-- Replace this
                if (_mainWindow != null)
                    break;
            }

            Assert.That(_mainWindow, Is.Not.Null, "Main window not found");
        }

        [TearDown]
        public void TearDown()
        {
            if (_process != null)
            {
                if (!_process.HasExited)
                    _process.Kill();
                _process.Dispose();
            }
        }

        [Test]
        public void SelectCheckedListBoxItem_ByName()
        {
            var checkedListBox = _mainWindow.FindFirst(TreeScope.Descendants,
                                new AndCondition(
                                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.List),
                                    new PropertyCondition(AutomationElement.AutomationIdProperty, "CheckedListBox")
                                ));
            Assert.That(checkedListBox, Is.Not.Null, "CheckedListBox not found.");

            var children = checkedListBox.FindAll(TreeScope.Children, Condition.TrueCondition);
            AutomationElement targetItem = null;
            foreach (AutomationElement child in children)
            {              
                if (child.Current.Name == "CheckBoxItem3")
                {
                    targetItem = child;
                    break;
                }
            }
            Assert.That(targetItem, Is.Not.Null, "CheckBoxItem3 not found.");

            // Get the TogglePattern and check the item
            var togglePattern = targetItem.GetCurrentPattern(TogglePattern.Pattern) as TogglePattern;
            Assert.That(togglePattern, Is.Not.Null, "TogglePattern not supported.");

            // Toggle to checked
            togglePattern.Toggle();
            // Assert the item is checked
            Assert.That(togglePattern.Current.ToggleState, Is.EqualTo(ToggleState.On), "Item was not checked.");

        }

        [Test]
        public void ExpandComboBox()
        {
            // Find the ComboBox control
            var comboBox = _mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox));

            Assert.That(comboBox, Is.Not.Null, "ComboBox not found.");
            // Expand the ComboBox using ExpandCollapsePattern

            var expandCollapse = comboBox.GetCurrentPattern(ExpandCollapsePattern.Pattern) as ExpandCollapsePattern;
            Assert.That(expandCollapse, Is.Not.Null, "ExpandCollapse pattern not found on ComboBox.");
            if (expandCollapse.Current.ExpandCollapseState != ExpandCollapseState.Expanded)
            {
                expandCollapse.Expand();
                //Thread.Sleep(1000);
            }
            Assert.That(expandCollapse.Current.ExpandCollapseState, Is.EqualTo(ExpandCollapseState.Expanded));          
        }
    }
}
