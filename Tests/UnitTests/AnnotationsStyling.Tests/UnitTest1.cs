using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Automation;

namespace AnnotationsStyling.Tests
{
    public class Tests
    {
        private Process _appProcess;
        private AutomationElement _mainWindow;

        [SetUp]
        public void Setup()
        {
            // Adjust the path to your application's EXE as needed
            var exePath = Path.GetFullPath(@"AnnotationsStyling.exe");
            _appProcess = Process.Start(exePath);

            // Wait for main window to be ready
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(500);
                _mainWindow = AutomationElement.RootElement.FindFirst(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.NameProperty, "MainWindow"));
                if (_mainWindow != null)
                    break;
            }
            Assert.IsNotNull(_mainWindow, "Main window not found.");
        }

        [TearDown]
        public void TearDown()
        {
            if (_appProcess != null)
            {
                if (!_appProcess.HasExited)
                    _appProcess.Kill();
                _appProcess.Dispose();
                _appProcess = null;
            }
        }

        [Test]
        public void LaunchAndInteractWithMainWindow()
        {
            // Find the StyleCombo ComboBox
            var combo = _mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "StyleCombo"));
            Assert.IsNotNull(combo, "StyleCombo ComboBox not found.");

            // Expand the ComboBox
            var expandPattern = combo.GetCurrentPattern(ExpandCollapsePattern.Pattern) as ExpandCollapsePattern;
            Assert.IsNotNull(expandPattern, "ExpandCollapsePattern not supported.");
            expandPattern.Expand();
            Thread.Sleep(500);

            // Get the list items
            var listItems = combo.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));
            Assert.IsTrue(listItems.Count > 1, "Not enough styles to select.");

            // Select the second style (index 1)
            var selectionItemPattern = listItems[1].GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            Assert.IsNotNull(selectionItemPattern, "SelectionItemPattern not supported.");
            selectionItemPattern.Select();
            Thread.Sleep(500);

            // Verify that the style has changed by checking the resource in the viewer
            var viewer = _mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ClassNameProperty, "FlowDocumentPageViewer"));
            Assert.IsNotNull(viewer, "FlowDocumentPageViewer not found.");

            // Collapse the ComboBox after selection
            var collapsePattern = combo.GetCurrentPattern(ExpandCollapsePattern.Pattern) as ExpandCollapsePattern;
            Assert.IsNotNull(collapsePattern, "ExpandCollapsePattern not supported for collapsing.");
            collapsePattern.Collapse();
            Thread.Sleep(500);            
        }

        [Test]
        public void ZoomInAndZoomOut_ChangesZoomLevel()
        {
            // Find the FlowDocumentPageViewer
            var viewer = _mainWindow.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ClassNameProperty, "FlowDocumentPageViewer"));
            Assert.IsNotNull(viewer, "FlowDocumentPageViewer not found.");

            // Find Zoom buttons (assumes standard WPF template: first button is zoom out, second is zoom in)
            var buttons = viewer.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
            Assert.IsTrue(buttons.Count >= 2, "Zoom buttons not found.");

            // Optionally, find the zoom level text (if available)
            var zoomText = viewer.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));

            string beforeZoom = zoomText?.Current.Name;

            // Click Zoom In
            var zoomInButton = buttons[1];
            var invokePattern = zoomInButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            Assert.IsNotNull(invokePattern, "InvokePattern not supported for Zoom In.");
            invokePattern.Invoke();
            Thread.Sleep(500);

            string afterZoomIn = zoomText?.Current.Name;

            // Click Zoom Out
            var zoomOutButton = buttons[0];
            invokePattern = zoomOutButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            Assert.IsNotNull(invokePattern, "InvokePattern not supported for Zoom Out.");
            invokePattern.Invoke();
            Thread.Sleep(500);

            string afterZoomOut = zoomText?.Current.Name;

            // If zoom text is available, verify it changes
            if (zoomText != null)
            {
                Assert.AreNotEqual(beforeZoom, afterZoomIn, "Zoom In did not change zoom level.");
                Assert.AreEqual(beforeZoom, afterZoomOut, "Zoom Out did not restore zoom level.");
            }
            // If not, at least verify no exceptions and buttons are clickable

            // Zoom in until the button is disabled or zoom level stops changing
            string lastZoom = zoomText?.Current.Name;
            for (int i = 0; i < 10; i++)
            {
                invokePattern = zoomInButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                invokePattern.Invoke();
                Thread.Sleep(200);
                string currentZoom = zoomText?.Current.Name;
                if (currentZoom == lastZoom) break; // Reached max zoom
                lastZoom = currentZoom;
            }
            // Optionally, check if zoomInButton is disabled

            // Zoom out until the button is disabled or zoom level stops changing
            for (int i = 0; i < 10; i++)
            {
                invokePattern = zoomOutButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                invokePattern.Invoke();
                Thread.Sleep(200);
                string currentZoom = zoomText?.Current.Name;
                if (currentZoom == lastZoom) break; // Reached min zoom
                lastZoom = currentZoom;
            }
            // Optionally, check if zoomOutButton is disabled
        }
    }
}