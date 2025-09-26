
using System.Windows;
using System.Windows.Controls;
using VisibiltyChanges;

namespace VisibilityChanges.Tests
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
        public void Button1_Click_Should_Set_TextBox_Visible()
        {
            var visibleButton = _window.FindName("btn1") as Button;
            Assert.That(visibleButton, Is.Not.Null, "Button not found.");
            visibleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var txtBox = _window.FindName("tb1") as TextBox;
            // Assert
            Assert.That(txtBox, Is.Not.Null, "Textbox not found.");
            Assert.That(txtBox.Visibility, Is.EqualTo(Visibility.Visible));
        }

        [Test]
        public void Button2_Click_Should_Set_TextBox_Hidden()
        {
            var visibleButton = _window.FindName("btn2") as Button;
            Assert.That(visibleButton, Is.Not.Null, "Button not found.");
            visibleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var txtBox = _window.FindName("tb1") as TextBox;
            // Assert
            Assert.That(txtBox, Is.Not.Null, "Textbox not found.");
            Assert.That(txtBox.Visibility, Is.EqualTo(Visibility.Hidden));
        }

        [Test]
        public void Button3_Click_Should_Set_TextBox_Collapsed()
        {
            var visibleButton = _window.FindName("btn3") as Button;
            Assert.That(visibleButton, Is.Not.Null, "Button not found.");
            visibleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var txtBox = _window.FindName("tb1") as TextBox;
            // Assert
            Assert.That(txtBox, Is.Not.Null, "Textbox not found.");
            Assert.That(txtBox.Visibility, Is.EqualTo(Visibility.Collapsed));
        }

        [Test]
        public void TextBox_Should_Accept_Text_Input()
        {
            // Arrange
            string expectedText = "Hello, WPF Test!";
            var txtBox = _window.FindName("tb1") as TextBox;
            Assert.That(txtBox, Is.Not.Null, "Textbox not found.");
            // Set focus to the textbox
            txtBox.Focus();
            txtBox.Clear();
            txtBox.AppendText(expectedText);
            Assert.That(expectedText, Is.EqualTo(txtBox.Text));
        }
    }
}
