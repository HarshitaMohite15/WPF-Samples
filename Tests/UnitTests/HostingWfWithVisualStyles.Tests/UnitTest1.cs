using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using Xunit;

namespace HostingWfWithVisualStyles.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void MainWindow_Tabs_CanBeClicked_And_Selected()
        {
            Exception threadEx = null;

            var thread = new Thread(() =>
            {
                try
                {
                    // Create the WPF window on an STA thread.
                    var window = new HostingWfWithVisualStyles.MainWindow();

                    // Ensure the window's Loaded handler runs (it wires up the WindowsFormsHost + TabControl).
                    window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                    // Find the Grid named "grid1" (created in the XAML code-behind).
                    var grid = window.FindName("grid1") as Grid;
                    Assert.NotNull(grid);

                    // Find the WindowsFormsHost that was added to the grid.
                    var host = grid.Children.OfType<WindowsFormsHost>().FirstOrDefault();
                    Assert.NotNull(host);

                    // Get the hosted WinForms TabControl.
                    var tabControl = host.Child as System.Windows.Forms.TabControl;
                    Assert.NotNull(tabControl);

                    // Verify there are two tabs as created in WindowLoaded.
                    Assert.True(tabControl.TabPages.Count >= 2, "Expected at least 2 tab pages.");

                    // "Click" (select) first tab and verify selection.
                    tabControl.SelectedIndex = 0;
                    Assert.Equal(0, tabControl.SelectedIndex);
                    Assert.Equal("Tab1", tabControl.SelectedTab!.Text);

                    // "Click" (select) second tab and verify selection.
                    tabControl.SelectedIndex = 1;
                    Assert.Equal(1, tabControl.SelectedIndex);
                    Assert.Equal("Tab2", tabControl.SelectedTab!.Text);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (threadEx != null)
            {
                // Surface assertion/exception thrown on the STA thread.
                throw new AggregateException("Exception occurred on STA test thread.", threadEx);
            }
        }
    }
}
