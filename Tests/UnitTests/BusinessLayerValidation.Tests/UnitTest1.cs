using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Threading;


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
                        BringWindowToFront(window);
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
            Thread.Sleep(200);

            // Signal the app to shutdown
            thread.Interrupt();
            // Wait for the thread to finish
            thread.Join(500);

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
                //// window.Focus();
                // window.Topmost = true;
                // window.Activate();                  // try to activate
                BringWindowToFront(window);
                window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

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
               // Thread.Sleep(200);
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
                BringWindowToFront(window);
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
        // Helper to bring a window to the foreground. Uses Topmost toggle + SetForegroundWindow.
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static void BringWindowToFront(Window window)
        {
            if (window == null) return;

            try
            {
                // Ensure activation + topmost briefly so the window comes forward
                window.ShowActivated = true;
                window.Topmost = true;
                window.Activate();
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

                // Try Win32 call as a fallback
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd != IntPtr.Zero)
                    SetForegroundWindow(hwnd);

                // Restore normal Topmost state
                window.Topmost = false;
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            }
            catch
            {
                // swallow: test host may restrict foreground changes
            }
        }
    }
}
