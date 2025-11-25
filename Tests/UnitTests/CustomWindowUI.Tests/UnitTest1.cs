using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NUnit.Framework;
using CustomWindowUI;

namespace CustomWindowUI.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CloseButton_ClosesWindow()
        {
            Exception? threadException = null;
            var finished = new ManualResetEventSlim();

            var uiThread = new Thread(() =>
            {
                try
                {
                    var app = new Application
                    {
                        ShutdownMode = ShutdownMode.OnExplicitShutdown
                    };

                    var window = new CustomWindowChrome();
                    window.Show();

                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

                    var closeButton = window.FindName("closeButton") as Button;
                    Assert.NotNull(closeButton, "closeButton not found in the window.");

                    closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (window.IsVisible && sw.ElapsedMilliseconds < 2000)
                    {
                        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                        Thread.Sleep(10);
                    }

                    Assert.IsFalse(window.IsVisible, "Window should be closed after clicking the close button.");

                    Assert.That(app.Windows.Count, Is.EqualTo(0), "Application should not have open windows.");

                    // Clean up
                    app.Shutdown();
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
                finally
                {
                    finished.Set();
                }
            });

            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.IsBackground = true;
            uiThread.Start();

            // Wait for the UI thread to finish (test timeout)
            if (!finished.Wait(TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("UI thread did not finish within the timeout.");
            }

            if (threadException != null)
            {
                // Rethrow so NUnit reports the actual failure
                throw new AggregateException("Exception in UI thread.", threadException);
            }
        }
    }
}