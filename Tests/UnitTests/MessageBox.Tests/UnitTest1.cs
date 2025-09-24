using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace MessageBox.Tests
{
    [Apartment(ApartmentState.STA)]
    public class Tests
    {
        private MainWindow _window;
        [SetUp]
        public void Setup()
        {
            _window = new MainWindow();
            _window.Show();
        }

        [TearDown]
        public void TearDown()
        {
            _window.Close();
        }

        [Test]
        public void ClickShowMessageBox()
        {
            string windowTitle = _window.Title;
            // Start a thread to close the message box automatically
            var messageCloser = new Thread(() =>
            {
                Thread.Sleep(500); // Wait for MessageBox to appear

                AutomationElement desktop = AutomationElement.RootElement;
                System.Windows.Automation.Condition condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);

                var msgBox = desktop.FindFirst(TreeScope.Children,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

                if (msgBox != null && msgBox.Current.Name == windowTitle) // Check if the MessageBox title matches the parent window
                {
                    var okButton = msgBox.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "OK"));
                    Assert.That(okButton, Is.Not.Null, "OK Button not found.");
                    var invoke = okButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                    invoke?.Invoke();
                }
            });
            messageCloser.Start();
            var checkBox = _window.FindName("ownerCheckBox") as CheckBox;
            Assert.That(checkBox, Is.Not.Null, "Checkbox not found.");
            checkBox.IsChecked = true;

            // Select a dropdown item (e.g., "OKCancel" in buttonComboBox)
            var buttonComboBox = _window.FindName("buttonComboBox") as ComboBox;
            Assert.That(buttonComboBox, Is.Not.Null, "buttonComboBox not found.");
            buttonComboBox.SelectedIndex = 1; // Select "OKCancel"

            // Validate selection
            var selectedItemImg = buttonComboBox.SelectedItem as ComboBoxItem;
            Assert.That(selectedItemImg, Is.Not.Null, "Selected item in buttonComboBox is null.");
            Assert.That((selectedItemImg.Content as string), Is.EqualTo("OKCancel"), "Selected icon is not 'OKCancel'.");

            // Select an icon in imageComboBox
            var imageComboBox = _window.FindName("imageComboBox") as ComboBox;
            Assert.That(imageComboBox, Is.Not.Null, "imageComboBox not found.");
            imageComboBox.SelectedIndex = 2; // Select "Exclamation"

            // Validate selection
            var selectedItem = imageComboBox.SelectedItem as ComboBoxItem;
            Assert.That(selectedItem, Is.Not.Null, "Selected item in imageComboBox is null.");
            Assert.That((selectedItem.Content as string), Is.EqualTo("Exclamation"), "Selected icon is not 'Exclamation'.");

            var showButton = _window.FindName("showMessageBoxButton") as Button;
            Assert.That(showButton, Is.Not.Null, "Button not found.");
            showButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            //Thread.Sleep(500);
            messageCloser.Join();
        }
    }
}
