using CalculatorDemo;
using NUnit.Framework;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
namespace CalculatorDemo.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Add_TwoNumbers_DisplaysResult()
        {
            // Arrange
            var window = new CalculatorDemo.MainWindow();
            window.Show();

            // Find buttons and display
            var b2 = (Button)window.FindName("B2");
            var bPlus = (Button)window.FindName("BPlus");
            var b3 = (Button)window.FindName("B3");
            var bEqual = (Button)window.FindName("BEqual");
            var display = (TextBox)window.FindName("DisplayBox");

            // Act: 2 + 3 =
            b2.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            bPlus.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            b3.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            bEqual.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            // Assert
            Assert.That(display.Text, Does.Contain("5"));

            window.Close();
        }
        [Test]
        public void Divide_ByZero_ShowsErrorOrZero()
        {
            var window = new CalculatorDemo.MainWindow();
            window.Show();

            var b8 = (Button)window.FindName("B8");
            var bDivide = (Button)window.FindName("BDevide");
            var b0 = (Button)window.FindName("B0");
            var bEqual = (Button)window.FindName("BEqual");
            var display = (TextBox)window.FindName("DisplayBox");

            // Get the window title on the UI thread
            string windowTitle = window.Title;
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

                    if (okButton != null)
                    {
                        var invoke = okButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                        invoke?.Invoke();
                    }
                }
            });
            messageCloser.Start();
            // 8 / 0 =
            b8.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            bDivide.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            b0.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            bEqual.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            messageCloser.Join();

            Assert.That(display.Text == "0" || display.Text.Contains("Error"));

            window.Close();
        }
        
        [Test]
        public void Negate_Number_DisplaysResult()
        {
            var window = new CalculatorDemo.MainWindow();
            window.Show();

            var b5 = (Button)window.FindName("B5");
            var bPM = (Button)window.FindName("BPM");
            var display = (TextBox)window.FindName("DisplayBox");

            // 5 +/-
            b5.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            bPM.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.That(display.Text, Does.Contain("-5"));

            window.Close();
        }

        [Test]
        public void Sqrt_Number_DisplaysResult()
        {
            var window = new CalculatorDemo.MainWindow();
            window.Show();

            var b9 = (Button)window.FindName("B9");
            var bSqrt = (Button)window.FindName("BSqrt");
            var display = (TextBox)window.FindName("DisplayBox");

            // 9 sqrt
            b9.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            bSqrt.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.That(display.Text, Does.Contain("3"));

            window.Close();
        }

    }
}