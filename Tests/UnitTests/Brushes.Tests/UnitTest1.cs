using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Xml;

namespace Brushes.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class Tests
    {
        private Process _process;
        private AutomationElement _mainWindow;
        private bool _treeExpanded;

        [SetUp]
        public void SetUp()
        {
            var exePath = Path.GetFullPath(@"Brushes.exe");
            var startInfo = new ProcessStartInfo(exePath)
            {
                UseShellExecute = false,
                // Hint the OS to start the process maximized
                WindowStyle = ProcessWindowStyle.Maximized
            };

            _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start app.");

            // wait for main window
            if (!_process.WaitForInputIdle(5000))
            {
                Thread.Sleep(1000);
            }
            var sw = Stopwatch.StartNew();
            while (_process.MainWindowHandle == IntPtr.Zero && sw.ElapsedMilliseconds < 10000)
            {
                Thread.Sleep(200);
            }

            var hwnd = _process.MainWindowHandle;
            Assert.AreNotEqual(IntPtr.Zero, hwnd, "Main window handle not found.");
            _mainWindow = AutomationElement.FromHandle(hwnd);
            Assert.IsNotNull(_mainWindow, "Failed to get AutomationElement for main window.");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(true);
                }
            }
            catch
            {
                _process?.Dispose();
            }
        }
        //Tests
        [Test]
        public void ExpandExpander_ThenVerifyExpanded()
        {
            // 1) Expand the left expander (click header)
            var expander = WaitForElementByAutomationId(_mainWindow, "LeftExpander", TimeSpan.FromSeconds(5));
            Assert.IsNotNull(expander, "Left expander not found.");

            // Click the expander's clickable point (header) to open it
            if (!TryClickElement(expander))
            {
                ClickBoundingRectCenter(expander);
            }
            Thread.Sleep(300); // allow animation
            // Wait and verify the expander reports Expanded via ExpandCollapsePattern
            var expanded = WaitForExpanderState(expander, ExpandCollapseState.Expanded, TimeSpan.FromSeconds(2));
            Assert.That(expanded, Is.True, "Expander did not become Expanded after click.");
        }
        [Test]
        public void ClickBrushesAndVerifyAnimationStatusOnLeafPages()
        {
            // 1) Expand left drawer
            var expander = WaitForElementByAutomationId(_mainWindow, "LeftExpander", TimeSpan.FromSeconds(5));
            Assert.IsNotNull(expander, "LeftExpander not found.");
            if (!TryClickElement(expander)) ClickBoundingRectCenter(expander);
            Assert.That(WaitForExpanderState(expander, ExpandCollapseState.Expanded, TimeSpan.FromSeconds(2)), Is.True, "Expander did not expand.");
            // 2) Find and parse TOC xml to get leaf examples
            var tocCandidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sampleresources\\toc.xml"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\sampleresources\\toc.xml"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\sampleresources\\toc.xml"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\sampleresources\\toc.xml")
            };
            var tocPath = tocCandidates.FirstOrDefault(File.Exists);
            Assert.IsNotNull(tocPath, "TOC file not found. Update tocCandidates to point to sampleresources/toc.xml.");

            var leafEntries = ParseTocLeafExamples(tocPath);
            Assert.IsTrue(leafEntries.Count > 0, "No leaf examples found in TOC.");
            // 3) locate TreeView
            var tree = WaitForElementByControlType(_mainWindow, ControlType.Tree, TimeSpan.FromSeconds(5));
            Assert.IsNotNull(tree, "TreeView not found.");
            // 4) Iterate leaf entries: select TreeItem, wait for page, find shapes, click and verify
            foreach (var entry in leafEntries)
            {
                TestContext.WriteLine($"Processing leaf: Title='{entry.Title}', Uri='{entry.Uri}'");

                //var treeItem = FindTreeItemByName(tree, entry.Title) ?? FindTreeItemByNameContains(tree, entry.Title);
                var treeItem = FindTreeItemByTitle(tree, entry.Title);
                Assert.IsNotNull(treeItem, $"TreeItem for title '{entry.Title}' not found in TreeView.");

                // navigate
                if (treeItem.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selObj) && selObj is SelectionItemPattern sip)
                {
                    sip.Select();
                }
                else
                {
                    if (!TryClickElement(treeItem)) ClickBoundingRectCenter(treeItem);
                }

                // small delay for navigation
                Thread.Sleep(350);
                // expected page id derived from Uri if present
                if (string.IsNullOrEmpty(entry.Uri))
                {
                    TestContext.WriteLine($" Warning: No Uri for leaf '{entry.Title}'. Skipping.");
                    continue;
                }
                var expectedPageId = string.IsNullOrEmpty(entry.Uri) ? null : (entry.Uri.Replace('\\', '/'));
                var frameRoot = FindFrameElement();
                var searchRoot = frameRoot ?? _mainWindow;

                var frameId = new PropertyCondition(AutomationElement.AutomationIdProperty, "myFrame");
                var frame = _mainWindow.FindFirst(TreeScope.Descendants, frameId);
               Assert.That(frame.Current.Name, Is.EqualTo(expectedPageId).IgnoreCase,
                    $"Page not shown for '{entry.Title}' (expected id='{expectedPageId}').");
                // collect clickable visual elements on the page
                var cond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
                var desc = searchRoot.FindAll(TreeScope.Descendants, cond);
                var candidates = new List<AutomationElement>();
                for (int i = 0; i < desc.Count; i++)
                {
                    try
                    {
                        var el = desc[i];
                        // heuristics: visible bounding rect and non-empty control type / name
                        var rect = el.Current.BoundingRectangle;
                        if (rect.Width > 4 && rect.Height > 4)
                        {
                            // filter out container types unlikely to be shapes
                            if (el.Current.ClassName == "Button" && (el.Current.Name.Contains("Start Animation")))
                            {
                                candidates.Add(el);
                                continue;
                            }
                        }
                    }
                    catch { /* swallow transient UIA exceptions */ }
                }
                if (candidates.Count == 0)
                {
                    // Assert.Warn($"No candidate brush elements found on page '{entry.Title}'. Consider adding AutomationIds (e.g. 'Brush_*') to shapes you want tested.");
                    TestContext.WriteLine($" No candidate brush elements found on page '{entry.Title}'. Skipping.");
                    continue;
                }
                // Click each candidate and verify status if available
                foreach (var candidate in candidates)
                {
                    var aid = candidate.Current.AutomationId;
                    var name = candidate.Current.Name;
                    TestContext.WriteLine($" Clicking candidate (Name='{name}', AutomationId='{aid}')");
                    // Compute a sampling point: center above the button (rectangle sits above the button in layout).
                    var btnRect = candidate.Current.BoundingRectangle;
                    Assert.IsTrue(btnRect.Width > 0 && btnRect.Height > 0, "Button has empty bounding rectangle.");

                    // Find a visual element (likely the rectangle) directly above the button to sample.
                    // Falls back to sampling a point above the button center if none found.
                    var targetVisual = FindElementAboveButton(searchRoot, candidate);
                    int sampleX, sampleY;
                    //if (targetVisual != null)
                    //{
                    //    var pt = GetElementCenterPoint(targetVisual);
                    //    sampleX = pt.x;
                    //    sampleY = pt.y;
                    //}
                    //else
                    //{
                    // fallback: sample a point above the button
                    sampleX = (int)Math.Round(btnRect.Left + btnRect.Width / 2.0);
                    var sampleYAbove = (int)Math.Round(btnRect.Top - (btnRect.Height * 1.5)); // relative fallback
                    sampleY = sampleYAbove;
                    // }
                    // Sample color before click
                    var before = GetScreenPixelColor(sampleX, sampleY);
                    // Click the button (prefer InvokePattern)
                    bool invoked = false;
                    try
                    {
                        if (candidate.TryGetCurrentPattern(InvokePattern.Pattern, out var ip) && ip is InvokePattern invoke)
                        {
                            invoke.Invoke();
                            invoked = true;
                        }
                    }
                    catch { /* ignore */ }
                    if (!invoked)
                    {
                        if (!TryClickElement(candidate))
                        {
                            ClickBoundingRectCenter(candidate);
                        }
                    }
                    // Wait up to the animation duration + buffer for the pixel to change.
                    // Increase timeout if animations are longer on slow machines.
                    var animationTimeout = TimeSpan.FromSeconds(5);
                    var changed = WaitForPixelColorChange(sampleX, sampleY, before, animationTimeout);

                    var after = GetScreenPixelColor(sampleX, sampleY);

                    // Assert that the color changed (animation started)
                    Assert.IsFalse(before.ToArgb() == after.ToArgb(),
                        $"Pixel color did not change after clicking Start Animation button. Before={before}, After={after}. If layout differs, adjust verticalOffset or use an AutomationId on the rectangle.");
                }
            }
        }
        // Helper methods
        private static AutomationElement WaitForElementByAutomationId(AutomationElement root, string automationId, TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                var cond = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
                var el = root.FindFirst(TreeScope.Descendants, cond);
                if (el != null)
                {
                    return el;
                }
                Thread.Sleep(200);
            }
            return null;
        }
        private static bool TryClickElement(AutomationElement el)
        {
            try
            {
                // If control exposes InvokePattern, use it
                if (el.TryGetCurrentPattern(InvokePattern.Pattern, out var p) && p is InvokePattern invoke)
                {
                    invoke.Invoke();
                    return true;
                }

                // Try clickable point
                var pt = el.GetClickablePoint();
                ClickAt((int)Math.Round(pt.X), (int)Math.Round(pt.Y));
                return true;
            }
            catch
            {
                return false;
            }
        }
        private static void ClickBoundingRectCenter(AutomationElement el)
        {
            var rect = el.Current.BoundingRectangle;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                throw new InvalidOperationException($"Element '{el.Current.AutomationId}' has empty bounding rectangle and no clickable point.");
            }
            var centerX = (int)Math.Round(rect.Left + rect.Width / 2.0);
            var centerY = (int)Math.Round(rect.Top + rect.Height / 2.0);
            ClickAt(centerX, centerY);
        }
        private static bool WaitForExpanderState(AutomationElement expanderElement, ExpandCollapseState desiredState, TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                try
                {
                    if (expanderElement.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var obj) && obj is ExpandCollapsePattern pattern)
                    {
                        var current = pattern.Current.ExpandCollapseState;
                        if (current == desiredState)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // Some controls surface ExpandCollapse on a child. Try querying the element again (refresh)
                        var refreshed = AutomationElement.FromHandle((IntPtr)expanderElement.Current.NativeWindowHandle);
                        if (refreshed != null && refreshed.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var obj2) && obj2 is ExpandCollapsePattern pattern2)
                        {
                            if (pattern2.Current.ExpandCollapseState == desiredState)
                                return true;
                        }
                    }
                }
                catch
                {
                    // swallow transient exceptions and retry
                }
                Thread.Sleep(100);
            }
            return false;
        }
        private static List<(string Title, string Uri)> ParseTocLeafExamples(string tocFilePath)
        {
            var result = new List<(string, string)>();
            var doc = new XmlDocument();
            doc.Load(tocFilePath);

            var exampleNodes = doc.SelectNodes("//Example");
            if (exampleNodes == null) return result;

            foreach (XmlElement node in exampleNodes)
            {
                var childExamples = node.SelectNodes("Example");
                if (childExamples != null && childExamples.Count > 0) continue; // not a leaf

                var title = node.GetAttribute("Title") ?? node.GetAttribute("title");
                var uri = node.GetAttribute("Uri") ?? node.GetAttribute("uri");
                if (!string.IsNullOrWhiteSpace(title))
                {
                    result.Add((title.Trim(), uri?.Trim()));
                }
            }
            return result;
        }
        private static AutomationElement WaitForElementByControlType(AutomationElement root, ControlType t, TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                var el = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, t));
                if (el != null) return el;
                Thread.Sleep(150);
            }
            return null;
        }
        private AutomationElement FindTreeItemByTitle(AutomationElement treeRoot, string title)
        {
            if (treeRoot == null || string.IsNullOrEmpty(title)) return null;

            // Try direct TreeItem name match first
            var direct = FindTreeItemByName(treeRoot, title);
            if (direct != null) return direct;

            // Expand top-level items so children are realized (best-effort)
            try
            {
                // Expand tree items to depth 3 (top -> child -> grandchild). Increase if needed.
                if (!_treeExpanded)
                {
                    ExpandTreeToDepth(treeRoot, 3);
                    // mark expanded so we do not iterate again
                    _treeExpanded = true;
                }
                // Thread.Sleep(150);
            }
            catch
            {
                // ignore transient failures
            }
            // Search Text nodes (TextBlock inside TreeViewItem) and walk up to TreeItem parent
            var textCond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text);
            var texts = treeRoot.FindAll(TreeScope.Descendants, textCond);
            for (int i = 0; i < texts.Count; i++)
            {
                try
                {
                    var textEl = texts[i];
                    if (string.Equals(textEl.Current.Name?.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        var parent = TreeWalker.ControlViewWalker.GetParent(textEl);
                        while (parent != null)
                        {
                            if (parent.Current.ControlType == ControlType.TreeItem) return parent;
                            parent = TreeWalker.ControlViewWalker.GetParent(parent);
                        }
                    }
                }
                catch
                {
                    // ignore UIA transient errors
                }
            }

            // Fallback: contains-match on TreeItem names
            return FindTreeItemByNameContains(treeRoot, title);
        }
        private static AutomationElement FindTreeItemByName(AutomationElement treeRoot, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var cond = new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TreeItem),
                new PropertyCondition(AutomationElement.NameProperty, name));
            return treeRoot.FindFirst(TreeScope.Descendants, cond);
        }
        private void ExpandTreeToDepth(AutomationElement parent, int depth)
        {
            if (parent == null || depth < 0) return;

            var itemCond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TreeItem);
            var children = parent.FindAll(TreeScope.Children, itemCond);

            for (int i = 0; i < children.Count; i++)
            {
                var item = children[i];
                try
                {
                    if (item.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var obj) && obj is ExpandCollapsePattern exp)
                    {
                        try
                        {
                            if ((exp.Current.ExpandCollapseState != ExpandCollapseState.Expanded) || (exp.Current.ExpandCollapseState != ExpandCollapseState.LeafNode))
                            {
                                exp.Expand();
                                // small delay to let UI realize children
                                Thread.Sleep(80);
                            }
                        }
                        catch
                        {
                            // ignore transient expand failures
                        }
                    }

                    // Recurse into this item's children
                    if (depth > 0)
                    {
                        ExpandTreeToDepth(item, depth - 1);
                    }
                }
                catch
                {
                    // ignore UIA transient exceptions and continue
                }

            }
        }
        private static AutomationElement FindTreeItemByNameContains(AutomationElement treeRoot, string substring)
        {
            if (treeRoot == null || string.IsNullOrEmpty(substring)) return null;
            var allItems = treeRoot.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TreeItem));
            for (int i = 0; i < allItems.Count; i++)
            {
                try
                {
                    var n = allItems[i].Current.Name ?? string.Empty;
                    if (n.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0) return allItems[i];
                }
                catch { }
            }
            return null;
        }
        private AutomationElement FindFrameElement()
        {
            var byId = new PropertyCondition(AutomationElement.AutomationIdProperty, "myFrame");
            var frame = _mainWindow.FindFirst(TreeScope.Descendants, byId);
            if (frame != null) return frame;

            var docCond = new OrCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane));
            var candidate = _mainWindow.FindFirst(TreeScope.Descendants, docCond);
            return candidate;
        }
        private static AutomationElement FindElementAboveButton(AutomationElement root, AutomationElement button)
        {
            if (root == null || button == null) return null;

            var btnRect = button.Current.BoundingRectangle;
            if (btnRect.Width <= 0 || btnRect.Height <= 0) return null;

            var all = root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            AutomationElement best = null;
            double bestScore = -1;

            for (int i = 0; i < all.Count; i++)
            {
                try
                {
                    var el = all[i];
                    var r = el.Current.BoundingRectangle;
                    if (r.Width <= 4 || r.Height <= 4) continue;

                    // Must be above the button
                    if (r.Bottom >= btnRect.Top - 2) continue;

                    // require horizontal overlap
                    var overlap = Math.Min(r.Right, btnRect.Right) - Math.Max(r.Left, btnRect.Left);
                    if (overlap <= 0) continue;

                    var overlapRatio = overlap / Math.Min(r.Width, btnRect.Width);
                    var area = r.Width * r.Height;
                    var distance = Math.Max(1.0, btnRect.Top - r.Bottom); // avoid div by zero

                    // Score prefers large area, good overlap, and small distance
                    var score = overlapRatio * (area / distance);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = el;
                    }
                }
                catch
                {
                    // ignore transient UIA errors
                }
            }

            return best;
        }
        private static System.Drawing.Color GetScreenPixelColor(int x, int y)
        {
            IntPtr hdc = IntPtr.Zero;
            try
            {
                hdc = GetDC(IntPtr.Zero);
                var colorRef = GetPixel(hdc, x, y);
                // COLORREF is 0x00bbggrr
                var r = (int)(colorRef & 0x000000FF);
                var g = (int)((colorRef & 0x0000FF00) >> 8);
                var b = (int)((colorRef & 0x00FF0000) >> 16);
                return System.Drawing.Color.FromArgb(r, g, b);
            }
            finally
            {
                if (hdc != IntPtr.Zero) ReleaseDC(IntPtr.Zero, hdc);
            }
        }
        private static bool WaitForPixelColorChange(int x, int y, System.Drawing.Color before, TimeSpan timeout, int pollMs = 300)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                while (sw.Elapsed < timeout)
                {
                    Thread.Sleep(pollMs);
                    var current = GetScreenPixelColor(x, y);
                    if (current.ToArgb() != before.ToArgb())
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // ignore transient failures; caller will treat as failure if no change observed
            }
            return false;
        }
    
        private static void ClickAt(int x, int y)
        {
            // Bring target app to foreground (best-effort)
            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);

            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, UIntPtr.Zero);
            Thread.Sleep(20);
            mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, UIntPtr.Zero);
        }

        // Win32 click helpers
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        // Win32 interop
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}