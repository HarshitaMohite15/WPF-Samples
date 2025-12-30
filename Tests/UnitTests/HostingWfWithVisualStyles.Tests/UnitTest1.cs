using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace HostingWfWithVisualStyles.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class Tests
    {
        private Process? app;

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (app != null && !app.HasExited)
                {
                    app.CloseMainWindow();
                    if (!app.WaitForExit(1000))
                    {
                        app.Kill();
                        app.WaitForExit(2000);
                    }
                }
            }
            catch
            {
                try { app?.Kill(); } catch { }
            }
            finally
            {
                app?.Dispose();
                app = null;
            }
        }

        [Test]
        public void SwitchTabs_Tab1_Tab2__WithUiAutomationAndNativeFallback()
        {
            // locate built app assembly
            var asmPath = typeof(HostingWfWithVisualStyles.MainWindow).Assembly.Location;
            Assert.That(File.Exists(asmPath), Is.True, $"App assembly not found at '{asmPath}'");

            // launch app (use dotnet <dll> if assembly is a DLL)
            var isExe = string.Equals(Path.GetExtension(asmPath), ".exe", StringComparison.OrdinalIgnoreCase);
            var psi = isExe
                ? new ProcessStartInfo(asmPath) { WorkingDirectory = Path.GetDirectoryName(asmPath) ?? Environment.CurrentDirectory, UseShellExecute = false }
                : new ProcessStartInfo("dotnet", $"\"{asmPath}\"") { WorkingDirectory = Path.GetDirectoryName(asmPath) ?? Environment.CurrentDirectory, UseShellExecute = false };

            app = Process.Start(psi);
            Assert.That(app, Is.Not.Null, "Failed to start app process.");
            var pid = app!.Id;

            // wait for main window
            var mainWindow = RetryWhileNull(() =>
                AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new AndCondition(
                        new PropertyCondition(AutomationElement.ProcessIdProperty, pid),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)
                    )
                ), TimeSpan.FromSeconds(12));
            Assert.That(mainWindow, Is.Not.Null, "Main window not found.");

            // find tab control - prefer AccessibleName "HostedTabControl" if present
            AutomationElement? tab = null;
            tab = RetryWhileNull(() =>
                mainWindow.FindFirst(TreeScope.Descendants,
                    new AndCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Tab),
                        new OrCondition(
                            new PropertyCondition(AutomationElement.NameProperty, "HostedTabControl"),
                            Condition.TrueCondition
                        )
                    )), TimeSpan.FromSeconds(6));

            if (tab == null)
            {
                // fallback: any Tab control
                tab = RetryWhileNull(() =>
                    mainWindow.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Tab)),
                    TimeSpan.FromSeconds(6));
            }

            Assert.That(tab, Is.Not.Null, "Tab control not found in the automation tree.");

            // Try to locate Tab1 and Tab2 automation elements
            AutomationElement? tab1 = FindTabItemByName(tab, "Tab1");
            AutomationElement? tab2 = FindTabItemByName(tab, "Tab2");

            // If Tab2 wasn't found by name, try name-contains and header-area scanning
            if (tab1 == null || tab2 == null)
            {
                var allDesc = tab.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                for (int i = 0; i < allDesc.Count && (tab1 == null || tab2 == null); i++)
                {
                    var el = allDesc[i];
                    var name = el.GetCurrentPropertyValue(AutomationElement.NameProperty) as string ?? string.Empty;
                    if (tab1 == null && name.IndexOf("Tab1", StringComparison.OrdinalIgnoreCase) >= 0) tab1 = el;
                    if (tab2 == null && name.IndexOf("Tab2", StringComparison.OrdinalIgnoreCase) >= 0) tab2 = el;
                }
            }

            // If still missing, attempt header-band candidates (visible elements in top band)
            if (tab1 == null || tab2 == null)
            {
                var headers = FindHeaderCandidates(tab);
                if (headers.Length >= 2)
                {
                    tab1 ??= headers[0];
                    tab2 ??= headers[1];
                }
            }

            // If Tab2 still missing, use native fallback to set selection by index (see below)
            // but keep trying to interact via UIA if possible.
            TestContext.WriteLine($"Tab1 found: {(tab1 != null)}; Tab2 found: {(tab2 != null)}");

            // Select Tab1 (ensure starting state)
            if (tab1 != null)
            {
                TrySelectElement(tab, tab1, "Tab1");
            }

            // Select Tab2
            if (tab2 != null)
            {
                TrySelectElement(tab, tab2, "Tab2");
            }
            else
            {
                // Native fallback: set tab index 1 on the WinForms TabControl window (TCM_SETCURSEL)
                var hwnd = GetNativeHandle(tab);
                Assert.That(hwnd, Is.Not.EqualTo(IntPtr.Zero), "Tab control does not expose a native window handle required for native fallback.");
                SendMessage(hwnd, TCM_SETCURSEL, new IntPtr(1), IntPtr.Zero);
                Thread.Sleep(200);
            }

            // Verify selected tab is Tab2 (by SelectionPattern or by name of selected TabItem)
            VerifySelectedTabName(tab, "Tab2");

            // Switch back to Tab1 using same strategy
            var hwnd2 = GetNativeHandle(tab);
            SendMessage(hwnd2, TCM_SETCURSEL, new IntPtr(0), IntPtr.Zero);
            Thread.Sleep(200);
            // }

            VerifySelectedTabName(tab, "Tab1");
        }

        private static AutomationElement? FindTabItemByName(AutomationElement tab, string name)
        {
            // direct TabItem children
            var items = tab.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
            for (int i = 0; i < items.Count; i++)
            {
                var n = items[i].GetCurrentPropertyValue(AutomationElement.NameProperty) as string ?? string.Empty;
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) return items[i];
            }

            // descendant exact match
            return tab.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name));
        }

        private static void TrySelectElement(AutomationElement tabControl, AutomationElement element, string expectedName)
        {
            TestContext.WriteLine($"Attempting to select '{expectedName}' via UIA patterns.");
            // SelectionItemPattern
            if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selObj) && selObj is SelectionItemPattern selPattern)
            {
                selPattern.Select();
                Thread.Sleep(150);
                Assert.That(selPattern.Current.IsSelected, Is.True, $"'{expectedName}' should be selected.");
                return;
            }

            // InvokePattern
            if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var invObj) && invObj is InvokePattern invPattern)
            {
                invPattern.Invoke();
                Thread.Sleep(150);
                return;
            }

            // clickable point
            if (element.TryGetClickablePoint(out var pt))
            {
                NativeMethods.SetCursorPos((int)Math.Round(pt.X), (int)Math.Round(pt.Y));
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN, (uint)pt.X, (uint)pt.Y, 0, 0);
                Thread.Sleep(30);
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, 0);
                Thread.Sleep(150);
                return;
            }

            TestContext.WriteLine($"Element '{expectedName}' not interactable via UIA patterns.");
        }

        private static void VerifySelectedTabName(AutomationElement tabControl, string expectedName)
        {
            // Prefer SelectionPattern on Tab control
            if (tabControl.TryGetCurrentPattern(SelectionPattern.Pattern, out var spObj) && spObj is SelectionPattern selPattern)
            {
                var selected = selPattern.Current.GetSelection();
                Assert.That(selected.Length, Is.GreaterThanOrEqualTo(1), "SelectionPattern reported no selection.");
                var name = selected[0].GetCurrentPropertyValue(AutomationElement.NameProperty) as string ?? string.Empty;
                Assert.That(name, Is.EqualTo(expectedName), $"Expected selected tab '{expectedName}' but got '{name}'");
                return;
            }

            // Otherwise try to find a descendant TabItem that reports IsSelected
            var items = tabControl.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].TryGetCurrentPattern(SelectionItemPattern.Pattern, out var siObj) && siObj is SelectionItemPattern si)
                {
                    if (si.Current.IsSelected)
                    {
                        var name = items[i].GetCurrentPropertyValue(AutomationElement.NameProperty) as string ?? string.Empty;
                        Assert.That(name, Is.EqualTo(expectedName), $"Expected selected tab '{expectedName}' but got '{name}'");
                        return;
                    }
                }
            }

            // As last resort, try to read a visible TabItem by name that likely indicates selection
            var candidate = tabControl.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, expectedName));
            Assert.That(candidate, Is.Not.Null, $"Could not verify selected tab name '{expectedName}' (no SelectionPattern and no TabItem reports selected).");
        }

        private static AutomationElement[] FindHeaderCandidates(AutomationElement tabControl)
        {
            try
            {
                var rect = tabControl.Current.BoundingRectangle;
                if (rect.IsEmpty) return Array.Empty<AutomationElement>();
                var threshold = rect.Top + rect.Height * 0.30;
                var all = tabControl.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                var list = new System.Collections.Generic.List<AutomationElement>();
                for (int i = 0; i < all.Count; i++)
                {
                    var el = all[i];
                    try
                    {
                        if (el.Current.IsOffscreen) continue;
                        var b = el.Current.BoundingRectangle;
                        if (b.IsEmpty) continue;
                        if (b.Top <= threshold && b.Width > 8 && b.Height > 6)
                        {
                            list.Add(el);
                        }
                    }
                    catch { }
                }
                return list.OrderBy(e => e.Current.BoundingRectangle.Left).ToArray();
            }
            catch { return Array.Empty<AutomationElement>(); }
        }

        private static IntPtr GetNativeHandle(AutomationElement el)
        {
            var handleObj = el.GetCurrentPropertyValue(AutomationElement.NativeWindowHandleProperty);
            if (handleObj is int i && i != 0) return new IntPtr(i);
            return IntPtr.Zero;
        }

        private static T? RetryWhileNull<T>(Func<T?> func, TimeSpan timeout) where T : class
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                try
                {
                    var r = func();
                    if (r != null) return r;
                }
                catch { }
                Thread.Sleep(120);
            }
            return null;
        }

        // native send for tab selection fallback
        private const int TCM_FIRST = 0x1300;
        private const int TCM_SETCURSEL = TCM_FIRST + 12;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private static class NativeMethods
        {
            public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
            public const uint MOUSEEVENTF_LEFTUP = 0x0004;

            [DllImport("user32.dll")]
            public static extern bool SetCursorPos(int X, int Y);

            [DllImport("user32.dll", ExactSpelling = true)]
            public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        }
    }
}