using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


namespace BusinessLayerValidation.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void App_Launches_MainWindow_Successfully()
        {
            Exception threadException = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var app = new App();
                    var window = new MainWindow();
                    app.Startup += (s, e) =>
                    {
                        Assert.IsNotNull(window);
                        Assert.IsTrue(window.IsLoaded == false);
                        window.Show();
                    };
                    app.Run();
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            // Give the window time to open
            Thread.Sleep(1000);

            // Signal the app to shutdown
            thread.Interrupt();
            // Wait for the thread to finish
            thread.Join(2000);

            Assert.IsNull(threadException, $"App threw exception: {threadException}");
        }

        [Test]
        public void AgeTextBox_ValidInput()
        {
            string? errorTooltip = null;
            var thread = new Thread(() =>
            {
                var window = new MainWindow();
                window.Show();

                // Find the TextBox (assumes it's the only one in the window)
                var ageTextBox = FindVisualChild<TextBox>(window);
                Assert.IsNotNull(ageTextBox, "Age TextBox not found");

                // Set valid input (e.g., numeric)
                ageTextBox.Text = "30";

                // Force binding update
                var binding = BindingOperations.GetBindingExpression(ageTextBox, TextBox.TextProperty);
                binding?.UpdateSource();

                // Allow UI to process validation
                window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

                // Check for validation error and tooltip
                bool hasError = Validation.GetHasError(ageTextBox);
                errorTooltip = ageTextBox.ToolTip as string;

                window.Close();

                Assert.IsFalse(hasError);
                Assert.IsNull(errorTooltip, "Error tooltip should not be set for valid input.");
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Test]
        public void AgeTextBox_InvalidInput_ShowsValidationError()
        {
            string? errorTooltip = null;
            var thread = new Thread(() =>
            {
                var window = new MainWindow();
                window.Show();

                // Find the TextBox (assumes it's the only one in the window)
                var ageTextBox = FindVisualChild<TextBox>(window);
                Assert.IsNotNull(ageTextBox, "Age TextBox not found");

                // Set invalid input (e.g., non-numeric)
                ageTextBox.Text = "invalid";

                // Force binding update
                var binding = BindingOperations.GetBindingExpression(ageTextBox, TextBox.TextProperty);
                binding?.UpdateSource();

                // Allow UI to process validation
                window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

                // Check for validation error and tooltip
                bool hasError = Validation.GetHasError(ageTextBox);
                errorTooltip = ageTextBox.ToolTip as string;

                window.Close();

                Assert.IsTrue(hasError, "Validation error was not triggered for invalid input.");
                Assert.IsFalse(string.IsNullOrEmpty(errorTooltip), "Error tooltip was not set.");
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        // Helper to find the first child of a given type in the visual tree
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    return tChild;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

    }
}
