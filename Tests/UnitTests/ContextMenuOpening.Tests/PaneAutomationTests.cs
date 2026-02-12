using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ContextMenuOpening.Tests
{
    public class PaneAutomationTests
    {
        // P/Invoke for synthetic mouse actions
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private static void DoEvents()
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new System.Windows.Threading.DispatcherOperationCallback(delegate (object f)
                {
                    ((System.Windows.Threading.DispatcherFrame)f).Continue = false;
                    return null;
                }),
                frame);
            System.Windows.Threading.Dispatcher.PushFrame(frame);
        }

        private static T[] FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return Array.Empty<T>();
            var list = new System.Collections.Generic.List<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) list.Add(t);
                list.AddRange(FindVisualChildren<T>(child));
            }
            return list.ToArray();
        }

        [Test, Apartment(ApartmentState.STA)]
        public void RightClick_PurpleRectangle_Should_Show_ContextMenu()
        {
            if (Application.Current == null)
            {
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            var pane = new Pane(); // calls InitializeComponent()
            var window = new Window { Content = pane };
            window.Show();
            try
            {
                // Show the window and wait for it to finish loading/rendering to ensure the XAML visual tree is materialized.
                var loaded = new ManualResetEventSlim();
                void OnContentRendered(object s, EventArgs e) { loaded.Set(); window.ContentRendered -= OnContentRendered; }
                window.ContentRendered += OnContentRendered;

                // window.Show();
                window.Activate();

                // Wait up to a short timeout for ContentRendered; fall back to pumping dispatcher.
                if (!loaded.Wait(TimeSpan.FromSeconds(2)))
                {
                    // If ContentRendered didn't fire in time, allow the dispatcher to process layout/rendering.
                    DoEvents();
                    window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }

                DoEvents();

                // Find Rectangle elements in visual tree. Search from the window to be robust.
                var rectangles = FindVisualChildren<Rectangle>(window);
                Assert.IsTrue(rectangles.Length >= 2, $"Expected at least two Rectangle elements in Pane, but found {rectangles.Length}.");
                var target = rectangles.ElementAt(0); // Purple rectangle 
                Assert.IsNotNull(target, "Target rectangle not found.");

                // Calculate center point in screen coordinates
                var bounds = target.TransformToAncestor(window).TransformBounds(new Rect(new Point(0, 0), target.RenderSize));
                var center = new Point(bounds.X + bounds.Width / 2.0, bounds.Y + bounds.Height / 2.0);
                var screenPoint = window.PointToScreen(center);

                // Act - move cursor and send a native right-click
                SetCursorPos((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y));
                Thread.Sleep(50); // give OS time to move cursor
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                // Allow handler to execute
                DoEvents();
                Thread.Sleep(100);

                // AFTER right-click: ContextMenu should be created by HandlerForCMO
                Assert.IsNotNull(target.ContextMenu, "ContextMenu should not be null after right-click on red rectangle.");
                Assert.That(target.ContextMenu.Items.Count, Is.EqualTo(4), "ContextMenu should have 3 items after right-click.");

                var items = target.ContextMenu.Items.Cast<MenuItem>().Select(mi => (string)mi.Header).ToArray();
                CollectionAssert.AreEqual(new[] { "Item1", "Item2", "Item3", "Item4" }, items, "ContextMenu should contain Item1, Item2, Item3.");
            }
            finally
            {
                // Close any open menus and the window
                DoEvents();
                window.Close();
            }
        }
        [Test, Apartment(ApartmentState.STA)]
        public void RedRectangle_ContextMenu_Should_Be_Created_On_RightClick()
        {
            if (Application.Current == null)
            {
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            var pane = new Pane();
            var window = new Window { Content = pane, Width = 800, Height = 600, WindowStartupLocation = WindowStartupLocation.CenterScreen };

            try
            {
                var loaded = new ManualResetEventSlim();
                void OnContentRendered(object s, EventArgs e) { loaded.Set(); window.ContentRendered -= OnContentRendered; }
                window.ContentRendered += OnContentRendered;

                window.Show();
                window.Activate();

                if (!loaded.Wait(TimeSpan.FromSeconds(2)))
                {
                    DoEvents();
                    window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }

                DoEvents();

                var rectangles = FindVisualChildren<Rectangle>(window);
                Assert.IsTrue(rectangles.Length >= 2, $"Expected at least two Rectangle elements, but found {rectangles.Length}.");
                var redRectangle = rectangles.ElementAt(1); // Red rectangle wired to HandlerForCMO
                Assert.IsNotNull(redRectangle, "Red rectangle not found.");

                // BEFORE right-click: ContextMenu should be null (not defined in XAML)
                Assert.IsNull(redRectangle.ContextMenu, "ContextMenu should be null before right-click on red rectangle.");

                // Perform right-click
                var bounds = redRectangle.TransformToAncestor(window).TransformBounds(new Rect(new Point(0, 0), redRectangle.RenderSize));
                var center = new Point(bounds.X + bounds.Width / 2.0, bounds.Y + bounds.Height / 2.0);
                var screenPoint = window.PointToScreen(center);

                SetCursorPos((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y));
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);

                // Allow handler to execute
                DoEvents();
                Thread.Sleep(100);

                // AFTER right-click: ContextMenu should be created by HandlerForCMO
                Assert.IsNotNull(redRectangle.ContextMenu, "ContextMenu should not be null after right-click on red rectangle.");
                Assert.That(redRectangle.ContextMenu.Items.Count, Is.EqualTo(3), "ContextMenu should have 3 items after right-click.");

                var items = redRectangle.ContextMenu.Items.Cast<MenuItem>().Select(mi => (string)mi.Header).ToArray();
                CollectionAssert.AreEqual(new[] { "Item1", "Item2", "Item3" }, items, "ContextMenu should contain Item1, Item2, Item3.");
            }
            finally
            {
                DoEvents();
                window.Close();
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void YellowRectangle_ContextMenu_Should_Be_Replaced_On_RightClick()
        {
            if (Application.Current == null)
            {
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            var pane = new Pane();
            var window = new Window { Content = pane, Width = 800, Height = 600, WindowStartupLocation = WindowStartupLocation.CenterScreen };

            try
            {
                var loaded = new ManualResetEventSlim();
                void OnContentRendered(object s, EventArgs e) { loaded.Set(); window.ContentRendered -= OnContentRendered; }
                window.ContentRendered += OnContentRendered;

                window.Show();
                window.Activate();

                if (!loaded.Wait(TimeSpan.FromSeconds(2)))
                {
                    DoEvents();
                    window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }

                DoEvents();

                var rectangles = FindVisualChildren<Rectangle>(window);
                Assert.IsTrue(rectangles.Length >= 2, $"Expected at least two Rectangle elements, but found {rectangles.Length}.");
                var yellowRectangle = rectangles.ElementAt(2); // yellow rectangle wired to HandlerForCMO
                Assert.IsNotNull(yellowRectangle, "Red rectangle not found.");

                // BEFORE right-click: ContextMenu should be null (not defined in XAML)
                Assert.IsNotNull(yellowRectangle.ContextMenu, "ContextMenu should be null before right-click on red rectangle.");
                var contextMenuBefore = yellowRectangle.ContextMenu;
                // Perform right-click
                var bounds = yellowRectangle.TransformToAncestor(window).TransformBounds(new Rect(new Point(0, 0), yellowRectangle.RenderSize));
                var center = new Point(bounds.X + bounds.Width / 2.0, bounds.Y + bounds.Height / 2.0);
                var screenPoint = window.PointToScreen(center);

                SetCursorPos((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y));
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);

                // Allow handler to execute
                DoEvents();
                Thread.Sleep(100);

                // AFTER right-click: ContextMenu should be created by HandlerForCMO
                Assert.IsNotNull(yellowRectangle.ContextMenu, "ContextMenu should not be null after right-click on red rectangle.");
                Assert.That(contextMenuBefore, Is.Not.EqualTo(yellowRectangle.ContextMenu));
                Assert.That(yellowRectangle.ContextMenu.Items.Count, Is.EqualTo(3), "ContextMenu should have 3 items after right-click.");

                var items = yellowRectangle.ContextMenu.Items.Cast<MenuItem>().Select(mi => (string)mi.Header).ToArray();
                CollectionAssert.AreEqual(new[] { "Item1", "Item2", "Item3" }, items, "ContextMenu should contain Item1, Item2, Item3.");
            }
            finally
            {
                DoEvents();
                window.Close();
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void MyButton_ContextMenu_Should_Be_Created_Via_Override_OnContextMenuOpening()
        {
            if (Application.Current == null)
            {
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            var pane = new Pane();
            var window = new Window { Content = pane, Width = 800, Height = 600, WindowStartupLocation = WindowStartupLocation.CenterScreen };

            try
            {
                var loaded = new ManualResetEventSlim();
                void OnContentRendered(object s, EventArgs e) { loaded.Set(); window.ContentRendered -= OnContentRendered; }
                window.ContentRendered += OnContentRendered;

                window.Show();
                window.Activate();

                if (!loaded.Wait(TimeSpan.FromSeconds(2)))
                {
                    DoEvents();
                    window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }

                DoEvents();

                // Find MyButton in the visual tree
                var myButtons = FindVisualChildren<MyButton>(window);
                Assert.That(myButtons.Length, Is.EqualTo(1), "Expected exactly one MyButton element in Pane.");
                var myButton = myButtons[0];
                Assert.IsNotNull(myButton, "MyButton not found.");

                // BEFORE right-click: ContextMenu should be null (no XAML definition, override hasn't run yet)
                Assert.IsNull(myButton.ContextMenu, "ContextMenu should be null before right-click on MyButton.");

                // Perform right-click on MyButton
                var bounds = myButton.TransformToAncestor(window).TransformBounds(new Rect(new Point(0, 0), myButton.RenderSize));
                var center = new Point(bounds.X + bounds.Width / 2.0, bounds.Y + bounds.Height / 2.0);
                var screenPoint = window.PointToScreen(center);

                SetCursorPos((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y));
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);

                // Allow OnContextMenuOpening override to execute
                DoEvents();
                Thread.Sleep(100);

                // AFTER right-click: ContextMenu should be created by MyButton.OnContextMenuOpening override
                Assert.IsNotNull(myButton.ContextMenu, "ContextMenu should not be null after right-click on MyButton.");
                Assert.That(myButton.ContextMenu.Items.Count, Is.EqualTo(3), "ContextMenu should have 3 items after right-click.");

                var items = myButton.ContextMenu.Items.Cast<MenuItem>().Select(mi => (string)mi.Header).ToArray();
                CollectionAssert.AreEqual(new[] { "Item1", "Item2", "Item3" }, items,
                    "ContextMenu created by MyButton override should contain Item1, Item2, Item3.");
            }
            finally
            {
                DoEvents();
                window.Close();
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void GreenRectangle_ContextMenu_Should_Be_Forced_Open_On_RightClick()
        {
            if (Application.Current == null)
            {
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            var pane = new Pane();
            var window = new Window { Content = pane, Width = 800, Height = 600, WindowStartupLocation = WindowStartupLocation.CenterScreen };

            try
            {
                var loaded = new ManualResetEventSlim();
                void OnContentRendered(object s, EventArgs e) { loaded.Set(); window.ContentRendered -= OnContentRendered; }
                window.ContentRendered += OnContentRendered;

                window.Show();
                window.Activate();

                if (!loaded.Wait(TimeSpan.FromSeconds(2)))
                {
                    DoEvents();
                    window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }

                DoEvents();

                var rectangles = FindVisualChildren<Rectangle>(window);
                Assert.IsTrue(rectangles.Length >= 4, $"Expected at least four Rectangle elements, but found {rectangles.Length}.");
                var greenRectangle = rectangles.ElementAt(3); // Green rectangle wired to HandlerForCMO2
                Assert.IsNotNull(greenRectangle, "Green rectangle not found.");

                // BEFORE right-click: ContextMenu should be null (not defined in XAML)
                Assert.IsNull(greenRectangle.ContextMenu, "ContextMenu should be null before right-click on green rectangle.");

                // Perform right-click
                var bounds = greenRectangle.TransformToAncestor(window).TransformBounds(new Rect(new Point(0, 0), greenRectangle.RenderSize));
                var center = new Point(bounds.X + bounds.Width / 2.0, bounds.Y + bounds.Height / 2.0);
                var screenPoint = window.PointToScreen(center);

                SetCursorPos((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y));
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);

                // Allow handler to execute
                DoEvents();
                Thread.Sleep(100);

                // AFTER first right-click: ContextMenu should be created by HandlerForCMO2 and deliberately forced open
                Assert.IsNotNull(greenRectangle.ContextMenu, "ContextMenu should not be null after right-click on green rectangle.");
                Assert.That(greenRectangle.ContextMenu.Items.Count, Is.EqualTo(3), "ContextMenu should have 3 items after right-click.");
                Assert.IsTrue(greenRectangle.ContextMenu.IsOpen, "ContextMenu should be forced open (IsOpen = true) by HandlerForCMO2.");

                var items = greenRectangle.ContextMenu.Items.Cast<MenuItem>().Select(mi => (string)mi.Header).ToArray();
                CollectionAssert.AreEqual(new[] { "Item1", "Item2", "Item3" }, items, "ContextMenu should contain Item1, Item2, Item3.");

                // Capture the ContextMenu instance to verify it's not recreated
                var contextMenuInstance = greenRectangle.ContextMenu;

                // Close the menu before second click test
                greenRectangle.ContextMenu.IsOpen = false;
                DoEvents();
                Thread.Sleep(50);

                // Second right-click: HandlerForCMO2 should NOT recreate menu (flag prevents re-execution)
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                DoEvents();
                Thread.Sleep(100);

                // Verify the ContextMenu was NOT recreated (same instance)
                Assert.That(greenRectangle.ContextMenu, Is.SameAs(contextMenuInstance),
                    "ContextMenu instance should be the same (not recreated) after second right-click.");
                Assert.That(greenRectangle.ContextMenu.Items.Count, Is.EqualTo(3),
                    "ContextMenu should still have 3 items (not recreated).");
            }
            finally
            {
                DoEvents();
                window.Close();
            }
        }
    }
}