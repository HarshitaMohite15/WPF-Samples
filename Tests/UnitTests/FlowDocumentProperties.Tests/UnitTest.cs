using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace FlowDocumentProperties.Tests
{
    [Apartment(ApartmentState.STA)]
    public class Test1
    {
        private MainWindow _window;
        private FlowDocument _fd;

        [SetUp]
        public void SetUp()
        {
            _window = new MainWindow();
            _window.Show();
            _fd = (FlowDocument)_window.FindName("fd1");
        }

        [TearDown]
        public void TearDown()
        {
            _window.Close();
        }
        [Test]
        public void SetRedBackgroundColor()
        {
            var backgroundProp = (System.Windows.UIElement)_window.FindName("backgroundProp");
            var tb2 = (System.Windows.Controls.TextBlock)_window.FindName("tb2");
            
            var sp1 = (StackPanel)_window.FindName("btns");
            var btn = sp1.Children.OfType<Button>().FirstOrDefault(b => b.Content?.ToString() == "Set Background");
            btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            // Assert
            Assert.That(backgroundProp.Visibility, Is.EqualTo(System.Windows.Visibility.Visible));
            Assert.That(tb2.Text, Does.Contain("Foreground and Background colors"));

            // Find the backgroundProp grid
            var backgroundProp1 = (Grid)_window.FindName("backgroundProp");

            // Find the StackPanel containing the radio buttons
            var radioPanel = (StackPanel)backgroundProp1.Children[2];

            // Find the "Red" radio button (first in the panel)
            var redRadioButton = (RadioButton)radioPanel.Children[0];

            // Simulate a user click by setting IsChecked to true
          
            redRadioButton.IsChecked = !(redRadioButton.IsChecked ?? false);
            redRadioButton.RaiseEvent(new RoutedEventArgs(RadioButton.ClickEvent));

            // Assert the background is now red
            Assert.That(_fd.Background, Is.EqualTo(Brushes.Red));
        }

        [Test]
        public void SetFontFamily()
        {
            var backgroundProp = (System.Windows.UIElement)_window.FindName("backgroundProp");
            var tb2 = (System.Windows.Controls.TextBlock)_window.FindName("tb2");

            var sp1 = (StackPanel)_window.FindName("btns");
            var btn = sp1.Children.OfType<Button>().FirstOrDefault(b => b.Content?.ToString() == "Set FontFamily");
            btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var fontFamilyBefore = _fd.FontFamily;
            // Assert
            Assert.That(backgroundProp.Visibility, Is.EqualTo(System.Windows.Visibility.Collapsed));
            Assert.That(tb2.Text,Is.Empty);
            var backgroundProp1 = (Grid)_window.FindName("backgroundProp");
            // Find the font family grid
            var fontFamilyGrid = (Grid)backgroundProp1.FindName("fontfamilyProp");

            // Find the StackPanel containing the radio buttons
            var radioPanel = (StackPanel)fontFamilyGrid.Children[2];

            // Find the "Red" radio button (first in the panel)
            var redRadioButton = (RadioButton)radioPanel.Children[1];

            // Simulate a user click by setting IsChecked to true

            redRadioButton.IsChecked = !(redRadioButton.IsChecked ?? false);
            redRadioButton.RaiseEvent(new RoutedEventArgs(RadioButton.ClickEvent));

            Assert.That(_fd.FontFamily, Is.Not.EqualTo(fontFamilyBefore));
            Assert.That(_fd.FontFamily, Is.EqualTo(new FontFamily("Verdana")));
        }
    }
}
