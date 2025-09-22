using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Xml.Linq;

namespace DragDropOpenTextFile.Tests
{
    [Apartment(ApartmentState.STA)]
    public class Tests
    {
        private MainWindow _window;
        [SetUp]
        public void Setup()
        {
            _window = new MainWindow();
            _window.Show();
        }
        [TearDown]
        public void TearDown()
        {
            _window.Close();
        }

            [Test]
        public void TextWrap()
        {
            var docTextBox = (TextBox)_window.FindName("tbDisplayFileContents");
            var originalText = docTextBox.Text;
            var textContent = "WPF lets you develop an application using both markup and code-behind, an experience with which ASP.NET developers should be familiar. You generally use XAML markup to implement the appearance of an application while using managed programming languages (code-behind) to implement its behavior.";
            docTextBox.Text = textContent;
            CheckBox wrapCheckBox = (CheckBox)_window.FindName("cbWrap");
            wrapCheckBox.IsChecked = !(wrapCheckBox.IsChecked ?? false);
            wrapCheckBox.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));

            Assert.That(docTextBox.Text, Is.Not.EqualTo(originalText));
            Assert.That(wrapCheckBox.IsChecked, Is.True);
            Assert.That(docTextBox.TextWrapping, Is.EqualTo(TextWrapping.Wrap));

        }

        [Test]
        public void DragDrop_SingleTextFile_UIAutomation()
        {
            // Arrange
            var docTextBox = (TextBox)_window.FindName("tbDisplayFileContents");
            string tempFile = Path.GetTempFileName();
            string fileContent = "Hello, drag-and-drop!";
            File.WriteAllText(tempFile, fileContent);

            // Create a DataObject with the file path
            var data = new DataObject(DataFormats.FileDrop, new string[] { tempFile });

            // Simulate drag-and-drop by invoking the drop handler directly
            var dragEventArgsType = typeof(System.Windows.DragEventArgs);

            var ctor = dragEventArgsType.GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .First(c => c.GetParameters().Length == 5);
            var dragArgs = ctor.Invoke(new object[] {
                data,
                0, // keyStates
                DragDropEffects.Copy, // allowedEffects
                docTextBox, // target
                new Point(0, 0) // point
            });

            // Set RoutedEvent to PreviewDropEvent
            var routedEventProp = dragArgs.GetType().GetProperty("RoutedEvent");
            Assert.That(routedEventProp, Is.Not.Null, "Method 'RoutedEvent' not found in MainWindow.");
            routedEventProp.SetValue(dragArgs, UIElement.PreviewDropEvent);

            // Act
            var ehDropMethod = typeof(MainWindow).GetMethod("EhDrop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(ehDropMethod, Is.Not.Null, "Method 'EhDrop' not found in MainWindow."); // Fix CS8602 by adding null check
            ehDropMethod.Invoke(_window, new object[] { docTextBox, dragArgs });

            // Assert
            Assert.That(docTextBox.Text, Is.EqualTo(fileContent));

            // Cleanup
            File.Delete(tempFile);
        }

    }
}
