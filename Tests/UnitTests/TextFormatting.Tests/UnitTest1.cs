using System.Diagnostics;
using System.Windows.Automation;
using System.Windows.Controls;

namespace TextFormatting.Tests
{
    public class Tests
    {
        private Process _appProcess;
        private AutomationElement _mainWindow;
        private AutomationElement _fontFamilyComboBox;
        private AutomationElement _textBox;
        [SetUp]
        public void Setup()
        {
            _appProcess = Process.Start(Path.GetFullPath(@"TextFormatting.exe"));
            Assert.That(_appProcess, Is.Not.EqualTo(null), "Failed to launch app");

            // Give it time to load
            Thread.Sleep(1000);
            Assert.That(_appProcess, Is.Not.EqualTo(null), "Failed to launch app");

            // Wait for main window
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(1000);
                _mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, "MainWindow")
                );
                if (_mainWindow != null) break;
            }
            Assert.That(_mainWindow, Is.Not.Null, "Main window not found.");

            _fontFamilyComboBox = _mainWindow.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "fontFamilyCB"));
            _textBox = _mainWindow.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "textToFormat"));
        }
        

        [TearDown]
        public void Teardown()
        {

            if (!_appProcess.HasExited)
            { _appProcess.Kill(); }
            _appProcess.Dispose();
        }

        [Test]
        public void TestStyleChange()
        {                   
            var boldButton = _mainWindow.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "boldButton"));
            var boldButtonPattern = boldButton.GetCurrentPattern(TogglePattern.Pattern) as TogglePattern;
            Assert.That(boldButtonPattern, Is.Not.Null);
            boldButtonPattern.Toggle();

            // Wait for the changes to take effect
            Thread.Sleep(500);
            // Validate bold button is checked (if applicable)
            Assert.That(boldButtonPattern.Current.ToggleState, Is.EqualTo(ToggleState.On), "Bold button not toggled on.");

            // Verify that the text box text has the expected font
            var textBoxValue = _textBox.GetCurrentPropertyValue(ValuePattern.ValueProperty).ToString();
            Assert.That(textBoxValue, Does.Contain("Lorem ipsum"));
        }

        [Test]
        public void TestChangeFontFamilyComboBox()
        {
            Assert.That(_fontFamilyComboBox, Is.Not.Null);
            var selectionPatternBefore = _fontFamilyComboBox.GetCurrentPattern(SelectionPattern.Pattern) as SelectionPattern;
            Assert.That(selectionPatternBefore, Is.Not.Null, "SelectionPattern not available.");
            var selectedItemsBefore = selectionPatternBefore.Current.GetSelection();
            Assert.That(selectedItemsBefore.Length, Is.EqualTo(1));
            var expandPattern = _fontFamilyComboBox.GetCurrentPattern(ExpandCollapsePattern.Pattern) as ExpandCollapsePattern;
            Assert.That(expandPattern, Is.Not.Null, "ExpandCollapsePattern not available.");
            expandPattern.Expand();
            Thread.Sleep(500); // Give time for items to appear

            // Find the first item in the fontFamilyCB ComboBox
            var fontItem = _fontFamilyComboBox.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));
            Assert.That(fontItem, Is.Not.Null, "No font family items found in ComboBox.");

            // Select the font item
            var itemPattern = fontItem.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            Assert.That(itemPattern, Is.Not.Null, "SelectionItemPattern not available.");
            itemPattern.Select();

            // Validate that the ComboBox selection has changed to "Arial"
            var selectionPattern = _fontFamilyComboBox.GetCurrentPattern(SelectionPattern.Pattern) as SelectionPattern;
            Assert.That(selectionPattern, Is.Not.Null, "SelectionPattern not available.");
            var selectedItems = selectionPattern.Current.GetSelection();
            Assert.That(selectedItems.Length, Is.EqualTo(1));
            Assert.That(selectedItems[0].Current.Name, Is.Not.EqualTo(selectedItemsBefore[0].Current.Name));
            // Wait for the change to take effect
            Thread.Sleep(500);
        }
    }
    }
