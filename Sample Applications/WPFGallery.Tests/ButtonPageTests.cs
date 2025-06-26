using NUnit.Framework;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using WPFGallery.Views;
using WPFGallery.ViewModels;
using System.Windows.Input;

namespace WPFGallery.Tests
{
    [Apartment(ApartmentState.STA)]
    [TestFixture]
    public class ButtonPageTests
    {
        private Window _window;
        private ButtonPage? _buttonPage;
        private Button? button;
        private Button? accentButton;
        
        [SetUp]
        public void SetUp()
        {             
            var viewModel = new ButtonPageViewModel();
            _buttonPage = new ButtonPage(viewModel);  
            _window = new Window
            {
                Content = _buttonPage,
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            _window.Show();
            // Allow UI to render  
            Thread.Sleep(500);

            button = _buttonPage.FindName("StandardWpfButton") as Button;

            accentButton = _buttonPage.FindName("AccentWpfButton") as Button;
        }

        [TearDown]
        public void TearDown()
        {
            _window.Close();
        }

        [Test]
        public void Button_ShouldExist_WithAutomationName()
        {
            Assert.IsNotNull(button, "Button named 'StandardWpfButton' should exist.");
        }

        [Test]
        public void Button_Click_ShouldUpdateLabelContent()
        {
            // Find the button and label by name          
            var label = _buttonPage?.FindName("Output") as Label;

            Assert.IsNotNull(button, "Button 'StandardWpfButton' should exist.");
            Assert.IsNotNull(label, "Label 'Output' should exist.");

            // Simulate button click
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            // Assert label content was updated
            Assert.IsFalse(string.IsNullOrEmpty(label.Content?.ToString()), "Label content should be updated after button click.");
        }

        [Test]
        public void Button_MouseEnter_And_MouseLeave_ShouldNotThrow()
        {            
            Assert.IsNotNull(button, "Button 'StandardWpfButton' should exist.");

            // Simulate MouseEnter
            Assert.DoesNotThrow(() =>
            {
                button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseEnterEvent
                });
            }, "MouseEnter event should not throw.");

            // Simulate MouseLeave
            Assert.DoesNotThrow(() =>
            {
                button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseLeaveEvent
                });
            }, "MouseLeave event should not throw.");
        }

        [Test]
        public void Button_PaddingProperties_ShouldBeSet()
        {
            //var button = _buttonPage?.FindName("StandardWpfButton") as Button;
            Assert.IsNotNull(button, "Button 'StandardWpfButton' should exist.");

            // Check Padding (default is 0 unless set elsewhere)
            Assert.AreEqual(new Thickness(11,5,11,6), button.Padding, "Padding should be set to default.");         
        }

        [Test]
        public void Button_ThicknessProperties_ShouldBeSet()
        {
            //var button = _buttonPage?.FindName("StandardWpfButton") as Button;
            Assert.IsNotNull(button, "Button 'StandardWpfButton' should exist.");

            // Check Padding (default is 0 unless set elsewhere)
            Assert.AreEqual(new Thickness(1), button.BorderThickness, "Thickness should be set to default.");
        }

        [Test]
        public void Button_Background_ShouldBeSet()
        {            
            Assert.IsNotNull(button, "Button 'StandardWpfButton' should exist.");

            // Check Background (default is #B3FFFFFF unless set elsewhere) 
            var expectedColor = Color.FromArgb(179, 255, 255, 255);
            var actualBrush = button.Background as SolidColorBrush;
            Assert.IsNotNull(actualBrush, "Button background should be a SolidColorBrush.");
            Assert.AreEqual(expectedColor, actualBrush.Color, "Background color should match the new default value.");
        }

        //[Test]
        //public void AccentButton_Background_ShouldMatchAccentTextFillColorTertiaryBrush()
        //{
        //    Assert.IsNotNull(accentButton, "Button 'AccentWpfButton' should exist.");

        //    // Get the expected brush from resources
        //    var brush = accentButton.TryFindResource("AccentTextFillColorTertiaryBrush") as SolidColorBrush;
        //    Assert.IsNotNull(brush, "Resource 'AccentTextFillColorTertiaryBrush' should exist.");

        //    var actualBrush = accentButton.Background as SolidColorBrush;
        //    Assert.IsNotNull(actualBrush, "AccentButton background should be a SolidColorBrush.");

        //    Assert.AreEqual(brush.Color, actualBrush.Color, "AccentButton background color should match AccentTextFillColorTertiaryBrush.");
        //}

        [Test]
        public void Button_FontProperties_ShouldBeSet()
        {
            //var button = _buttonPage?.FindName("StandardWpfButton") as Button;
            Assert.IsNotNull(button, "Button 'StandardWpfButton' should exist.");

            // Check FontSize (default is 12.0 unless set elsewhere)
            Assert.Greater(button.FontSize, 0, "FontSize should be greater than 0.");

            // Check FontFamily (default is 'Segoe UI' unless set elsewhere)
            Assert.IsNotNull(button.FontFamily, "FontFamily should not be null.");
            Assert.IsNotEmpty(button.FontFamily.Source, "FontFamily should have a source.");
        }

        [Test]
        public void Button_Height_And_Width_ShouldBeGreaterThanZero()
        {
            //var button = _buttonPage.FindName("StandardWpfButton") as Button;
            Assert.IsNotNull(button, "Button 'StandardWpfButton' should exist.");

            // Height and Width may be NaN if not explicitly set, so check ActualHeight/ActualWidth
            Assert.Greater(button.ActualHeight, 0, "ActualHeight should be greater than 0.");
            Assert.Greater(button.ActualWidth, 0, "ActualWidth should be greater than 0.");
        }

        [Test]
        public void AccentButton_ShouldExist_WithAutomationName()
        {            
            Assert.IsNotNull(accentButton, "Button 'AccentWpfButton' should exist.");
        }

        [Test]
        public void AccentButton_MouseEnter_And_MouseLeave_ShouldNotThrow()
        {
            //var button = _buttonPage.FindName("AccentWpfButton") as Button;
            Assert.IsNotNull(accentButton, "Button 'AccentWpfButton' should exist.");

            Assert.DoesNotThrow(() =>
            {
                accentButton.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseEnterEvent
                });
            }, "MouseEnter event should not throw.");

            Assert.DoesNotThrow(() =>
            {
                accentButton.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0)
                {
                    RoutedEvent = UIElement.MouseLeaveEvent
                });
            }, "MouseLeave event should not throw.");
        }

        [Test]
        public void AccentButton_FontProperties_ShouldBeSet()
        {
            //var button = _buttonPage.FindName("AccentWpfButton") as Button;
            Assert.IsNotNull(accentButton, "Button 'AccentWpfButton' should exist.");

            Assert.Greater(accentButton.FontSize, 0, "FontSize should be greater than 0.");
            Assert.IsNotNull(accentButton.FontFamily, "FontFamily should not be null.");
            Assert.IsNotEmpty(accentButton.FontFamily.Source, "FontFamily should have a source.");
        }

        [Test]
        public void AccentButton_Height_And_Width_ShouldBeGreaterThanZero()
        {
            //var button = _buttonPage.FindName("AccentWpfButton") as Button;
            Assert.IsNotNull(accentButton, "Button 'AccentWpfButton' should exist.");

            Assert.Greater(accentButton.ActualHeight, 0, "ActualHeight should be greater than 0.");
            Assert.Greater(accentButton.ActualWidth, 0, "ActualWidth should be greater than 0.");
        }

        private Button FindButtonByAutomationName(DependencyObject root, string automationName)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is Button btn)
                {
                    var peer = new ButtonAutomationPeer(btn);
                    var name = peer.GetName();
                    if (name == automationName)
                        return btn;
                }
                var result = FindButtonByAutomationName(child, automationName);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}