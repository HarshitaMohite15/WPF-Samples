using DialogBox;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
namespace DialogBox.Tests
{
    [Apartment(ApartmentState.STA)]
    public class Tests
    {
        private MainWindow _window;

        [SetUp]
        public void SetUp()
        {
            _window = new MainWindow();
            _window.Show();
        }

        [TearDown]
        public void TearDown()
        {
            var messageCloser = new Thread(() =>
            {
                Thread.Sleep(500); // Wait for MessageBox to appear

                AutomationElement desktop = AutomationElement.RootElement;
                System.Windows.Automation.Condition condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);

                AutomationElement msgBox = null;
                for (int i = 0; i < 10 && msgBox == null; i++)
                {
                    msgBox = desktop.FindFirst(
                        TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Word Processor")
                    );
                    if (msgBox == null) Thread.Sleep(100);
                }
                if (msgBox != null && msgBox.Current.Name == "Word Processor") // Check if the MessageBox title matches the parent window
                {
                    var noButton = msgBox.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "No"));

                    if (noButton != null)
                    {
                        var invoke = noButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                        invoke?.Invoke();
                    }
                }
            });
            messageCloser.Start();
            _window.Close();
            messageCloser.Join();
        }

        [Test]
        public void FormatFontMenuItem_Click_ChangesFont()
        {
            var docTextBox = (TextBox)_window.FindName("documentTextBox");
            var defaultFont = docTextBox.FontFamily.Source;
            // Start a thread to interact with the FontDialogBox when it appears
            var automationThread = new Thread(() =>
            {
                // Wait for the dialog to appear
                Thread.Sleep(1000);

                // Find the FontDialogBox window
                var desktop = AutomationElement.RootElement;
                AutomationElement fontDialog = null;
                for (int i = 0; i < 10 && fontDialog == null; i++)
                {
                    fontDialog = desktop.FindFirst(
                        TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Fonts")
                    );
                    if (fontDialog == null) Thread.Sleep(200);
                }
                Assert.That(fontDialog, Is.Not.Null, "Font dialog not found");
               
                // Find the font family ComboBox and select "Arial"
                var fontFamilyCombo = fontDialog.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "fontFamilyListBox"));
                Assert.That(fontFamilyCombo, Is.Not.Null, "fontFamilyListBox not found");

                var arialItem = fontFamilyCombo.FindFirst(TreeScope.Descendants,
                   new PropertyCondition(AutomationElement.NameProperty, "Arial"));
                Assert.That(arialItem, Is.Not.Null, "Arial font not found");
                var selectPattern = arialItem.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
                selectPattern?.Select();
               
                // Click OK
                var okButton = fontDialog.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "okButton"));
                Assert.That(okButton, Is.Not.Null, "OK button not found");
                var invoke = okButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                invoke?.Invoke();
            });
            automationThread.Start();

            // Simulate user clicking the Font... menu item
            var menuItem = (MenuItem)_window.FindName("formatFontMenuItem");
            Assert.That(menuItem, Is.Not.Null);
            menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
         
            automationThread.Join();

            Assert.That(docTextBox, Is.Not.Null);
            Assert.That(docTextBox.FontFamily.Source, Is.EqualTo("Arial"));
            Assert.That(docTextBox.FontFamily.Source, Is.Not.EqualTo(defaultFont));
            //Assert.That(docTextBox.FontSize, Is.EqualTo(20));
        }
     
    }

}
