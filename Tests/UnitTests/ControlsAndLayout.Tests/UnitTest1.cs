using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Xml;
namespace ControlsAndLayout.Tests
{
    public class Tests
    {
        private Process _appProcess;
        private AutomationElement _mainWindow;
        private const int DefaultTimeout = 5000;

        [SetUp]
        public void Setup()
        {
            // Start the application
            var exePath = Path.GetFullPath(@"ControlsAndLayout.exe");
            _appProcess = Process.Start(exePath);

            // Wait for the application to initialize
            _appProcess.WaitForInputIdle(DefaultTimeout);
            Thread.Sleep(1000);

            // Get the main window automation element
            _mainWindow = AutomationElement.FromHandle(_appProcess.MainWindowHandle);
            Assert.That(_mainWindow, Is.Not.Null, "Main window should be available");
        }

        [TearDown]
        public void TearDown()
        {
            if (_appProcess != null && !_appProcess.HasExited)
            {
                _appProcess.Kill();
                _appProcess.WaitForExit();
                _appProcess.Dispose();
            }
        }

        [Test]
        public void Expanders_ShouldExist()
        {
            // Arrange
            var expanderCondition = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.Group);

            // Act
            var expanders = _mainWindow.FindAll(TreeScope.Descendants, expanderCondition);

            // Assert
            Assert.That(expanders, Has.Count.GreaterThanOrEqualTo(2), "Should have at least 2 expanders (Layout and Controls)");
        }

        [Test]
        public void Expanders_ShouldExpandOnClick()
        {
            // Arrange
            var layoutExpander = FindExpanderByName("Layout");
            var controlsExpander = FindExpanderByName("Controls");
            Assert.That(layoutExpander, Is.Not.Null, "Layout expander should exist");
            Assert.That(controlsExpander, Is.Not.Null, "Controls expander should exist");
            // Act - Expand Layout expander
            ExpandIfCollapsed(layoutExpander);
            Thread.Sleep(500); // Wait for expansion animation
            // Assert - Verify Layout expander is expanded
            Assert.That(layoutExpander.TryGetCurrentPattern(
                ExpandCollapsePattern.Pattern, out object patternObject), Is.True, "Layout expander should support ExpandCollapsePattern");
            var expandCollapsePattern = patternObject as ExpandCollapsePattern;
            Assert.That(expandCollapsePattern, Is.Not.Null);
            Assert.That(expandCollapsePattern.Current.ExpandCollapseState, Is.EqualTo(ExpandCollapseState.Expanded),
                "Layout expander should be expanded");
            // Act - Expand Controls expander
            ExpandIfCollapsed(controlsExpander);
            Thread.Sleep(500); // Wait for expansion animation
            // Assert - Verify Controls expander is expanded
            Assert.That(controlsExpander.TryGetCurrentPattern(
                ExpandCollapsePattern.Pattern, out patternObject), Is.True, "Controls expander should support ExpandCollapsePattern");
            expandCollapsePattern = patternObject as ExpandCollapsePattern;
            Assert.That(expandCollapsePattern, Is.Not.Null);
            Assert.That(expandCollapsePattern.Current.ExpandCollapseState, Is.EqualTo(ExpandCollapseState.Expanded),
                "Controls expander should be expanded");
        }
        [Test]
        public void TextBox_ShouldRenderXAMLOnSelectionLayout()
        {
            // Arrange
            var layoutExpander = FindExpanderByName("Layout");
            Assert.That(layoutExpander, Is.Not.Null);
            ExpandIfCollapsed(layoutExpander);
            Thread.Sleep(500);

            var layoutListBox = FindElementByAutomationId("LayoutListBox");
            Assert.That(layoutListBox, Is.Not.Null);

            var listItemCondition = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.ListItem);
            var listItems = layoutListBox.FindAll(TreeScope.Children, listItemCondition);
            Assert.That(listItems, Is.Not.Empty);

            // Act - Select an item
            var firstItem = listItems[0];
            //var selectionPattern = firstItem.GetCurrentPattern(
            //    SelectionItemPattern.Pattern) as SelectionItemPattern;
            //Assert.That(selectionPattern, Is.Not.Null);
            //selectionPattern.Select();

            var rectObj = firstItem.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty, true);
            if (rectObj is System.Windows.Rect rect && !rect.IsEmpty)
            {
                var cx = (int)Math.Round(rect.Left + rect.Width / 2.0);
                var cy = (int)Math.Round(rect.Top + rect.Height / 2.0);
                SetCursorPos(cx, cy);
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)cx, (uint)cy, 0, UIntPtr.Zero);
                Thread.Sleep(30);
                mouse_event(MOUSEEVENTF_LEFTUP, (uint)cx, (uint)cy, 0, UIntPtr.Zero);
            }
            //var ok = TrySelectListBoxItemByIndex(layoutListBox, 2, 5000);
            //Assert.That(ok, Is.True, "Failed to select first item in ControlsListBox");

            Thread.Sleep(300); // Wait for XAML parsing and rendering

            var textBox = FindElementByAutomationId("TextBox1");
            Assert.That(textBox, Is.Not.Null, "TextBox1 should exist");

            var valuePattern = textBox.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
            Assert.That(valuePattern, Is.Not.Null);

            var selectedItemXaml = valuePattern.Current.Value ?? string.Empty;
            var selected = GetSelectedListBoxItem(layoutListBox);
            Assert.That(selected, Is.Not.Null, "Selected item should not be null");

            var title = GetListItemTitle(selected);
            Assert.That(title, Is.Not.Null, "Selected item's visible title should be available");

            // locate samples.xml next to the running exe
            var mainModule = _appProcess?.MainModule;
            Assert.That(mainModule, Is.Not.Null, "MainModule should not be null");
            var appDir = Path.GetDirectoryName(mainModule.FileName);
            Assert.That(appDir, Is.Not.Null, "Application directory should be available");
            var samplesPath = Path.Combine(appDir, "samples.xml");
            Assert.That(File.Exists(samplesPath), Is.True, $"samples.xml not found at {samplesPath}");

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(samplesPath);
            // find expected Syntax in samples.xml for Category[1]
            var xpath = $"/Samples/Category[1]/Sample[@Title='{EscapeForXPath(title)}'] | /Samples/Category[1]/Sample[Title='{EscapeForXPath(title)}']";
            var node = xmlDoc.SelectSingleNode(xpath) as XmlElement;
            Assert.That(node, Is.Not.Null, $"Sample node for title '{title}' not found in samples.xml");

            // Syntax may be attribute or child element
            string? expectedSyntax = null;
            if (node.HasAttribute("Syntax"))
                expectedSyntax = node.GetAttribute("Syntax");
            else
            {
                var syntaxChild = node.SelectSingleNode("Syntax");
                if (syntaxChild != null)
                    expectedSyntax = syntaxChild.InnerText;
            }
            Assert.That(expectedSyntax, Is.Not.Null, $"No Syntax found for sample '{title}'");
            Assert.That(expectedSyntax, Is.EqualTo(selectedItemXaml),
                "XAML loaded in TextBox1 should match expected Syntax from samples.xml");
        }
        [Test]
        public void TextBox_ShouldRenderXAMLOnSelectionControl()
        {
            // Arrange
            var controlsExpander = FindExpanderByName("Controls");
            Assert.That(controlsExpander, Is.Not.Null);
            ExpandIfCollapsed(controlsExpander);
            Thread.Sleep(500);

            var controlsListBox = FindElementByAutomationId("ControlsListBox");
            Assert.That(controlsListBox, Is.Not.Null);
            var listItemCondition = new PropertyCondition(
                AutomationElement.ControlTypeProperty, ControlType.ListItem);
            var listItems = controlsListBox.FindAll(TreeScope.Children, listItemCondition);
            Assert.That(listItems.Count, Is.GreaterThan(0));

            // Act - Select an item
            var firstItem = listItems[0];
            var selectionPattern = firstItem.GetCurrentPattern(
                SelectionItemPattern.Pattern) as SelectionItemPattern;
            Assert.That(selectionPattern, Is.Not.Null);
            selectionPattern.Select();
            Thread.Sleep(1000); // Wait for XAML parsing and rendering

            var textBox = FindElementByAutomationId("TextBox1");
            Assert.That(textBox, Is.Not.Null, "TextBox1 should exist");

            var valuePattern = textBox.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
            Assert.That(valuePattern, Is.Not.Null);

            var selectedItemXaml = valuePattern.Current.Value ?? string.Empty;
            var selected = GetSelectedListBoxItem(controlsListBox);
            Assert.That(selected, Is.Not.Null, "Selected item should not be null");

            var title = GetListItemTitle(selected);
            Assert.That(title, Is.Not.Null, "Selected item's visible title should be available");

            // locate samples.xml next to the running exe
            Assert.That(_appProcess.MainModule, Is.Not.Null, "MainModule should not be null");
            var appDir = Path.GetDirectoryName(_appProcess.MainModule.FileName);
            Assert.That(appDir, Is.Not.Null, "Application directory should be available");
            var samplesPath = Path.Combine(appDir, "samples.xml");
            Assert.That(File.Exists(samplesPath), Is.True, $"samples.xml not found at {samplesPath}");

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(samplesPath);
            // find expected Syntax in samples.xml for Category[2]
            var xpath = $"/Samples/Category[2]/Sample[@Title='{EscapeForXPath(title)}'] | /Samples/Category[2]/Sample[Title='{EscapeForXPath(title)}']";
            var node = xmlDoc.SelectSingleNode(xpath) as XmlElement;
            Assert.That(node, Is.Not.Null, $"Sample node for title '{title}' not found in samples.xml");

            // Syntax may be attribute or child element
            string? expectedSyntax = null;
            if (node.HasAttribute("Syntax"))
                expectedSyntax = node.GetAttribute("Syntax");
            else
            {
                var syntaxChild = node.SelectSingleNode("Syntax");
                if (syntaxChild != null)
                    expectedSyntax = syntaxChild.InnerText;
            }
            Assert.That(expectedSyntax, Is.Not.Null, $"No Syntax found for sample '{title}'");
            Assert.That(expectedSyntax, Is.EqualTo(selectedItemXaml),
                "XAML loaded in TextBox1 should match expected Syntax from samples.xml");
        }
        [Test]
        public void PreviewThenXaml_RadioToggle_UpdatesUI()
        {
            EnsureForeground();

            var radioPreview = FindRadioByName("Preview");
            var radioXaml = FindRadioByName("XAML");
            var previewArea = FindByAutomationId("PreviewArea");
            var codeBox = FindByAutomationId("TextBox1");

            Assert.That(radioPreview, Is.Not.Null, "Preview radio not found");
            Assert.That(radioXaml, Is.Not.Null, "XAML radio not found");
            Assert.That(codeBox, Is.Not.Null, "TextBox1 not found");

            // Ensure code view is visible first
            var rectObj = radioXaml.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty, true);
            if (!(rectObj is System.Windows.Rect rect) || rect.IsEmpty)
                throw new InvalidOperationException("Element has no usable bounding rectangle.");

            var x = (int)Math.Round(rect.Left + rect.Width / 2.0);
            var y = (int)Math.Round(rect.Top + rect.Height / 2.0);

            // Move and click
            SetCursorPos(x, y);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, UIntPtr.Zero);
            Thread.Sleep(30);
            mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            Assert.That(WaitUntil(() => IsRadioSelected(radioXaml), 100), Is.True, "XAML radio should be selected");

            Assert.That(WaitUntil(() => ElementIsVisible(codeBox), 100), Is.True, "TextBox1 should be visible after selecting XAML");
            // Switch to Preview and verify code hides and preview shows
            // ActivateRadio(radioPreview);
            var rectObjP = radioPreview.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty, true);
            if (!(rectObjP is System.Windows.Rect rectP) || rectP.IsEmpty)
                throw new InvalidOperationException("Element has no usable bounding rectangle.");

            var xP = (int)Math.Round(rectP.Left + rectP.Width / 2.0);
            var yP = (int)Math.Round(rectP.Top + rectP.Height / 2.0);

            // Move and click
            SetCursorPos(xP, yP);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)xP, (uint)yP, 0, UIntPtr.Zero);
            Thread.Sleep(30);
            mouse_event(MOUSEEVENTF_LEFTUP, (uint)xP, (uint)yP, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            Assert.That(WaitUntil(() => IsRadioSelected(radioPreview), 100), Is.True, "Preview radio should be selected");
            Assert.That(WaitUntil(() => ElementIsVisible(codeBox), 100), Is.False, "TextBox1 should not be visible after selecting XAML");
            // Wait for code area to be effectively hidden (removed/offscreen/tiny)
            Assert.That(IsElementEffectivelyHidden(codeBox), Is.True, "TextBox1 did not hide/collapse after selecting Preview");

            if (previewArea != null)
            {
                Assert.That(WaitUntil(() => ElementIsVisible(previewArea), 3000), Is.True, "PreviewArea should be visible after selecting Preview");
            }
            else
            {
                TestContext.Out.WriteLine("PreviewArea not exposed to UIA — skipping preview visibility assertion.");
            }

            // Switch back to XAML and verify preview hides and code shows
            var rectObjX = radioXaml.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty, true);
            if (!(rectObjX is System.Windows.Rect rectX) || rectX.IsEmpty)
                throw new InvalidOperationException("Element has no usable bounding rectangle.");

            var xL = (int)Math.Round(rectX.Left + rectX.Width / 2.0);
            var yL = (int)Math.Round(rectX.Top + rectX.Height / 2.0);

            // Move and click
            SetCursorPos(xL, yL);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)xL, (uint)yL, 0, UIntPtr.Zero);
            Thread.Sleep(30);
            mouse_event(MOUSEEVENTF_LEFTUP, (uint)xL, (uint)yL, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            Assert.That(WaitUntil(() => IsRadioSelected(radioXaml), 100), Is.True, "XAML radio should be selected");

            Assert.That(WaitUntil(() => ElementIsVisible(codeBox), 100), Is.True, "TextBox1 should be visible after selecting XAML");

            if (previewArea != null)
            {
                Assert.That(WaitUntilElementHidden("PreviewArea", 500), Is.True, "PreviewArea did not hide after selecting XAML (if exposed to UIA)");
            }
        }

        #region Helper Methods

        private bool IsElementEffectivelyHidden(AutomationElement el)
        {
            if (el == null) return true; // removed from UIA tree => hidden/collapsed

            try
            {
                // 1) IsOffscreen true is a good sign
                if (el.Current.IsOffscreen) return true;

                // 2) Bounding rectangle empty or tiny
                var rect = el.Current.BoundingRectangle;
                if (rect.IsEmpty) return true;
                if (rect.Height < 4.0 || rect.Width < 4.0) return true;

                // 3) Sometimes the element remains but its parents collapsed — try to see if top-left is outside window bounds
                var winRect = _mainWindow.Current.BoundingRectangle;
                if (rect.Bottom < winRect.Top || rect.Top > winRect.Bottom) return true;

                return false;
            }
            catch
            {
                // If any query fails, treat as not deterministically visible
                return true;
            }
        }
        private bool WaitUntilElementHidden(string automationId, int timeoutMs)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                try
                {
                    var el = _mainWindow.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
                    if (el == null)
                        return true; // removed from UIA tree -> hidden

                    // Use GetCurrentPropertyValue to force a fresh read
                    var isOffObj = el.GetCurrentPropertyValue(AutomationElement.IsOffscreenProperty, true);
                    if (isOffObj is bool isOff && isOff)
                        return true;

                    // Fallback: bounding rectangle small or empty
                    var rectObj = el.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty, true);
                    if (rectObj is System.Windows.Rect rect)
                    {
                        if (rect.IsEmpty || rect.Height < 4.0 || rect.Width < 4.0)
                            return true;
                    }
                }
                catch
                {
                    // treat exceptions as transient; continue polling
                }
            }
            return false;
        }
        private AutomationElement FindRadioByName(string name)
        {
            var cond = new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.RadioButton),
                new PropertyCondition(AutomationElement.NameProperty, name));
            return _mainWindow.FindFirst(TreeScope.Descendants, cond);
        }
        private void EnsureForeground()
        {
            try
            {
                SetForegroundWindow(_appProcess.MainWindowHandle);
                Thread.Sleep(150);
            }
            catch { }
        }
        private static bool IsRadioSelected(AutomationElement radio)
        {
            try
            {
                if (radio.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var sel))
                    return ((SelectionItemPattern)sel).Current.IsSelected;
                if (radio.TryGetCurrentPattern(TogglePattern.Pattern, out var tog))
                    return ((TogglePattern)tog).Current.ToggleState == ToggleState.On;
            }
            catch { }
            return false;
        }
        private AutomationElement FindElementByAutomationId(string automationId)
        {
            var condition = new PropertyCondition(
                AutomationElement.AutomationIdProperty, automationId);
            return _mainWindow.FindFirst(TreeScope.Descendants, condition);
        }

        private AutomationElement FindExpanderByName(string name)
        {
            // Expanders typically appear as Group control type
            var condition = new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Group),
                new PropertyCondition(AutomationElement.NameProperty, name));

            var expander = _mainWindow.FindFirst(TreeScope.Descendants, condition);

            if (expander == null)
            {
                // Try looking for it as a Pane control type (alternative in some cases)
                condition = new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane),
                    new PropertyCondition(AutomationElement.NameProperty, name));
                expander = _mainWindow.FindFirst(TreeScope.Descendants, condition);
            }

            return expander;
        }
        private void ExpandIfCollapsed(AutomationElement expanderElement)
        {
            object patternObject;
            if (expanderElement.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out patternObject) &&
                patternObject is ExpandCollapsePattern expandCollapsePattern)
            {
                if (expandCollapsePattern.Current.ExpandCollapseState is ExpandCollapseState.Collapsed or
                    ExpandCollapseState.PartiallyExpanded)
                {
                    expandCollapsePattern.Expand();
                }
            }
        }
        private static bool WaitUntil(Func<bool> condition, int timeoutMs)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (condition())
                    return true;
                Thread.Sleep(100);
            }
            return false;
        }

        private static bool ElementIsVisible(AutomationElement el)
        {
            try
            {
                return el != null && !el.Current.IsOffscreen && !el.Current.BoundingRectangle.IsEmpty;
            }
            catch { return false; }
        }

        private AutomationElement FindByAutomationId(string id)
        {
            return _mainWindow.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, id));
        }

        /// <summary>
        /// Returns the currently selected ListBoxItem AutomationElement or null if none.
        /// </summary>
        private static AutomationElement? GetSelectedListBoxItem(AutomationElement listBox)
        {
            if (listBox == null) return null;
            // 1) Preferred: ask the list container for its current selection (works with virtualization)
            try
            {
                if (listBox.TryGetCurrentPattern(SelectionPattern.Pattern, out var selPatternObj) &&
                    selPatternObj is SelectionPattern selPattern)
                {
                    var selection = selPattern.Current.GetSelection();
                    if (selection != null && selection.Length > 0)
                        return selection[0];
                }
            }
            catch
            {
                // ignore and try fallback
            }
            // 2) Fallback: scan descendant list items and check SelectionItemPattern
            try
            {
                var listItemCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem);
            var items = listBox.FindAll(TreeScope.Children, listItemCondition);
            foreach (AutomationElement item in items)
            {
                try
                {
                    if (item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pat))
                    {
                        var sip = pat as SelectionItemPattern;
                        if (sip != null && sip.Current.IsSelected)
                            return item;
                    }
                }
                catch
                {
                    // ignore and continue
                }
            }
            }
            catch
            {
                // final fallback - return null
            }
            return null;
        }

        /// <summary>
        /// Robustly selects an item by index: ScrollIntoView, SetFocus, SelectionItemPattern.Select() or click fallback.
        /// Waits until the selection is reflected in the list container SelectionPattern or the item reports IsSelected.
        /// </summary>
        private bool TrySelectListBoxItemByIndex(AutomationElement listBox, int index, int timeoutMs = 5000)
        {
            if (listBox == null) return false;
            var listItemCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem);
            var items = listBox.FindAll(TreeScope.Children, listItemCondition);
            if (index < 0 || index >= items.Count) return false;
            var item = items[index];

            // Scroll into view if supported
            try
            {
                if (item.TryGetCurrentPattern(ScrollItemPattern.Pattern, out var scrollObj) && scrollObj is ScrollItemPattern scrollPat)
                {
                    scrollPat.ScrollIntoView();
                    Thread.Sleep(150);
                }
            }
            catch { }
            // Set focus on the item
            try { item.SetFocus(); } catch { }

            // Try selection via SelectionItemPattern
            try
            {
                //if (item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selObj) && selObj is SelectionItemPattern sip)
                //{
                //    sip.Select();
                //}
                //else
                //{
                    // Fall back to clicking the center of the item's bounding rectangle
                    var rectObj = item.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty, true);
                    if (rectObj is System.Windows.Rect rect && !rect.IsEmpty)
                    {
                        var cx = (int)Math.Round(rect.Left + rect.Width / 2.0);
                        var cy = (int)Math.Round(rect.Top + rect.Height / 2.0);
                        SetCursorPos(cx, cy);
                        Thread.Sleep(50);
                        mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)cx, (uint)cy, 0, UIntPtr.Zero);
                        Thread.Sleep(30);
                        mouse_event(MOUSEEVENTF_LEFTUP, (uint)cx, (uint)cy, 0, UIntPtr.Zero);
                    }
                    else
                    {
                        // As a final fallback, try invoking selection on the container using SelectionPattern (if supports)
                        if (listBox.TryGetCurrentPattern(SelectionPattern.Pattern, out var sp) && sp is SelectionPattern)
                        {
                            // nothing specific to call; rely on UI reacting to focus/click attempts above
                        }
                    }
                //}
            }
            catch { }

            // Wait until selection is observed
            var expectedObserved = WaitUntil(() =>
            {
            try
            {
                // 1) Check container SelectionPattern
                if (listBox.TryGetCurrentPattern(SelectionPattern.Pattern, out var selPatternObj) &&
                    selPatternObj is SelectionPattern selPattern)
                {
                        var sel = selPattern.Current.GetSelection();
                        if (sel != null && sel.Length > 0 && sel[0].Equals(item))
                            return true;
                    }

                    // 2) Check item SelectionItemPattern
                    if (item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var sipObj) && sipObj is SelectionItemPattern sip2)
                    {
                        if (sip2.Current.IsSelected) return true;
                    }
                }
                catch { }
                return false;
            }, timeoutMs);

            return expectedObserved;
        }
        private static string? GetListItemTitle(AutomationElement listItem)
        {
            if (listItem == null) return null;

            try
            {
                var textCond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text);
                var textEl = listItem.FindFirst(TreeScope.Descendants, textCond);
                if (textEl != null)
                {
                    var name = textEl.Current.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                        return name.Trim();
                }

                // fallback to list item Name
                var fallback = listItem.Current.Name;
                return string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();
            }
            catch
            {
                return null;
            }
        }
        private string EscapeForXPath(string s) =>
           s.Contains("'") ? "concat('" + s.Replace("'", "',\"'\",'") + "')" : s;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        #endregion
    }
}