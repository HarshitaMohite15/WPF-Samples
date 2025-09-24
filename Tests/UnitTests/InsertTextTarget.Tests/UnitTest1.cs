using InsertTextW32Target;
using NUnit.Framework;
using System.Diagnostics;
using System.Windows.Automation;
using System.Windows.Forms;

namespace InsertTextTarget.Tests
{
    public class Tests
    {
        private Process process;
        private AutomationElement mainWindow;
        [SetUp]
        public void SetUp()
        {
            // Path to your built WinForms executable
            var appPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "InsertTextTarget.exe");
             process = Process.Start(appPath);
            Thread.Sleep(1000);

            // Get the main window
             mainWindow = AutomationElement.FromHandle(process.MainWindowHandle);
        }

        [TearDown]
        public void TearDown()
        {
            // Close the app
            if (!process.HasExited)
                process.Kill();
            process.Dispose();
        }

        [Test]
        public void CheckBox1_ShouldBeChecked_WhenClicked()
        {
                // Wait for main window to be ready
                Thread.Sleep(1000);

                // Find the checkbox by its AutomationId or Name
                var checkBox = mainWindow.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "checkBox1"));
                           
                Assert.That(checkBox, Is.Not.Null, "checkBox1 not found");
                var togglePattern = checkBox.GetCurrentPattern(TogglePattern.Pattern) as TogglePattern;
                Assert.That(togglePattern, Is.Not.Null, "TogglePattern not supported by checkBox1");
                togglePattern.Toggle();

                // Wait for UI to update
                Thread.Sleep(100);

                Assert.That(togglePattern.Current.ToggleState, Is.EqualTo(ToggleState.On), "checkBox1 should be checked after UI automation click.");
        }

        [Test]
        public void RichTextBox_ShouldBeEnabled_WhenCheckBox2IsUnchecked()
        {
            // Find the checkbox by its AutomationId or Name
            var checkBox2 = mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "checkBox2"));
            Assert.That(checkBox2, Is.Not.Null, "checkBox2 not found");
            var togglePattern = checkBox2.GetCurrentPattern(TogglePattern.Pattern) as TogglePattern;
            Assert.That(togglePattern, Is.Not.Null, "TogglePattern not supported by checkBox2");
            //check the checkbox2, default state is unchecked
            togglePattern.Toggle();
            // Wait for UI to update
            Thread.Sleep(100);
            Assert.That(togglePattern.Current.ToggleState, Is.EqualTo(ToggleState.On), "checkBox2 should be checked after UI automation click.");
            // Find the RichTextBox by its AutomationId or Name
            var rtbTarget = mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "rtbTarget"));
            Assert.That(rtbTarget, Is.Not.Null, "rtbTarget not found");
            Assert.That(rtbTarget.Current.IsEnabled, Is.False, "rtbTarget should be disabled when checkBox2 is checked.");

            // Uncheck the checkbox2
            togglePattern.Toggle();
            // Wait for UI to update
            Thread.Sleep(100);
            Assert.That(togglePattern.Current.ToggleState, Is.EqualTo(ToggleState.Off), "checkBox2 should be checked after UI automation click.");
            Assert.That(rtbTarget, Is.Not.Null, "rtbTarget not found");
            Assert.That(rtbTarget.Current.IsEnabled, Is.True, "rtbTarget should be enabled when checkBox2 is unchecked.");
        }

        [Test]
        public void SingleLineTextbox_AllowOnly2Characters()
        {
            var textBox = mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "tbTarget"));

            Assert.That(textBox, Is.Not.Null, "tbTarget not found");

            // Set focus to the textbox
            textBox.SetFocus();
            Thread.Sleep(100);

            // Send keys (simulate user typing)
            SendKeys.SendWait("ABCDE");
            Thread.Sleep(200);

            var valuePattern = textBox.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
            Assert.That(valuePattern, Is.Not.Null, "ValuePattern not supported by tbTarget");
            Assert.That(valuePattern.Current.Value, Is.EqualTo("AB"), "tbTarget should only allow 2 characters from user input.");
        }


    }


}
