using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using ClipboardViewer;

namespace ClipboardViewer.Tests
{
    [Apartment(ApartmentState.STA)]
    public class Tests
    {
        private Process _process; // Declare _process field
        private AutomationElement _mainWindow; // Declare _mainWindow field

        [SetUp]
        public void SetUp()
        {
         
            var exePath = Path.GetFullPath(@"ClipboardViewer.exe");
            Assert.That(File.Exists(exePath), $"Executable not found at: {exePath}");
            _process = Process.Start(exePath);

            // Wait for main window to be ready
            for (int i = 0; i < 20; i++)
            {
                _mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, "MainWindow"));
                if (_mainWindow != null)
                    break;
                Thread.Sleep(500);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (!_process.HasExited)
                _process.Kill();
            _process.Dispose();
        }

        [Test]
        public void CopyAndClearClipboardButton_IsPresent_AndClickable()
        {
            var button = _mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, "Copy To Clipboard"));
            Assert.That(button, Is.Not.Null, "Copy To Clipboard button not found.");

            var invokePattern = button.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            Assert.That(invokePattern, Is.Not.Null, "InvokePattern not supported.");
            invokePattern.Invoke();
            var clipboardTextBefore = Clipboard.GetText();
            Assert.That(clipboardTextBefore, Is.Not.Empty, "Clipboard is empty");
           
            var clearButton = _mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, "Clear Clipboard"));
            Assert.That(clearButton, Is.Not.Null, "Clear Clipboard button not found."); // Use Assert.That with Is.Not.Null

            var invokePatternClear = clearButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            Assert.That(invokePatternClear, Is.Not.Null, "InvokePattern not supported."); // Use Assert.That with Is.Not.Null
            invokePatternClear.Invoke();
            Thread.Sleep(500); // Wait a moment for clipboard to clear
            var clipboardText = Clipboard.GetText();
            Assert.That(string.IsNullOrEmpty(clipboardText), "Clipboard was not cleared.");

        }

        [Test]
        public void AllFormatCheckboxes_ArePresent_AndCanBeToggled()
        {
            string[] checkBoxNames = { "cbAudio", "cbFileDropList", "cbImage", "cbText", "cbRtf", "cbXaml" };
            foreach (var name in checkBoxNames)
            {
                var checkBox = _mainWindow.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, name));
                Assert.That(checkBox, Is.Not.Null, $"Checkbox {name} not found.");

                // Try to toggle if enabled
                if (!checkBox.Current.IsEnabled)
                    continue;

                var togglePattern = checkBox.GetCurrentPattern(TogglePattern.Pattern) as TogglePattern;
                Assert.That(togglePattern, Is.Not.Null, $"TogglePattern not supported for {name}.");
                var initialState = togglePattern.Current.ToggleState;
                togglePattern.Toggle();
                Assert.That(togglePattern.Current.ToggleState != initialState, $"Checkbox {name} did not toggle.");
            }
        }

        [Test]
        public void CopyAndPasteSpecificContent()
        {
            var copyButton = _mainWindow.FindFirst(TreeScope.Descendants,
                   new PropertyCondition(AutomationElement.NameProperty, "Copy To Clipboard"));
            Assert.That(copyButton, Is.Not.Null, "Copy To Clipboard button not found.");

            var copyInvoke = copyButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            Assert.That(copyInvoke, Is.Not.Null, "InvokePattern not supported for Copy button.");
            copyInvoke.Invoke();
            Thread.Sleep(500);
            // Arrange: Set a known value in the clipboard before launching the test
            const string testContent = "Hello, ClipboardViewer!";
            Clipboard.SetText(testContent);
            // Wait for clipboard to update
            Thread.Sleep(500);
            // Act: Click "Paste From Clipboard" to paste the content into the app
            var pasteButton = _mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, "Paste From Clipboard"));
            Assert.That(pasteButton, Is.Not.Null, "Paste From Clipboard button not found.");

            var pasteInvoke = pasteButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            Assert.That(pasteInvoke, Is.Not.Null, "InvokePattern not supported for Paste button.");
            pasteInvoke.Invoke();

            // Wait for UI to update
            Thread.Sleep(500);

            // Find the RichTextBox and verify the pasted content
            var richTextBox = _mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "richTextBox"));
            Assert.That(richTextBox, Is.Not.Null, "RichTextBox not found.");

            // Get the text from the RichTextBox using ValuePattern if available
            string pastedText = null;
            if (richTextBox.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePatternObj))
            {
                var valuePattern = valuePatternObj as ValuePattern;
                Assert.That(valuePattern, Is.Not.Null, "ValuePattern not supported for RichTextBox.");
                pastedText = valuePattern.Current.Value;
            }
            else
            {
                // If ValuePattern is not supported, fallback to legacy text pattern
                if (richTextBox.TryGetCurrentPattern(TextPattern.Pattern, out var textPatternObj))
                {
                    var textPattern = textPatternObj as TextPattern;
                    Assert.That(textPattern, Is.Not.Null, "TextPattern not supported for RichTextBox.");
                    pastedText = textPattern.DocumentRange.GetText(-1);
                }
            }

            Assert.That(pastedText, Is.Not.Null.And.Contains(testContent), "RichTextBox does not contain the pasted content.");

            // Assert: Clipboard should contain the test content
            string clipboardTextBefore = SafeGetClipboardText();
            Assert.That(clipboardTextBefore, Is.Not.Null, "Clipboard is empty");

            Assert.That(clipboardTextBefore, Is.Not.Null.And.Contains(testContent), "Clipboard does not contain the expected copied content.");
        }

        private string SafeGetClipboardText(int retries = 5, int delayMs = 100)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    return Clipboard.GetText();
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    Thread.Sleep(delayMs);
                }
            }
            throw new InvalidOperationException("Unable to access clipboard after multiple attempts.");
        }
    }
}
