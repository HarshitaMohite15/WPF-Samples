using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace EditingCollections.Tests
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
        public void AddButton_Click_AddsItem()
        {
            string newDescription = "TestItem";
            // Start a thread to close the message box automatically
            var messageCloser = new Thread(() =>
            {
                // Wait for the dialog to appear
                Thread.Sleep(1000);

                // Find the addItemWindow window
                var desktop = AutomationElement.RootElement;
                AutomationElement addItemWindow = null;
                for (int i = 0; i < 10 && addItemWindow == null; i++)
                {
                    addItemWindow = desktop.FindFirst(
                        TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "ChangeItem")
                    );
                    if (addItemWindow == null) Thread.Sleep(200);
                }
                Assert.That(addItemWindow, Is.Not.Null, "Add Item dialog not found");
                // Set the description
                var descBox = addItemWindow.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "Description"));
                Assert.That(descBox, Is.Not.Null, "Description TextBox not found.");
                var valuePattern = descBox.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
                Assert.That(valuePattern, Is.Not.Null, "ValuePattern not found on Description TextBox.");
                valuePattern.SetValue(newDescription);
                // Find and click the _Submit button
                var submitButton = addItemWindow.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "_Submit"));
                Assert.That(submitButton, Is.Not.Null, "_Submit button not found");
                // submitButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var invoke = submitButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                invoke?.Invoke();
            });
            messageCloser.Start();
            var addButton = _window.FindName("add") as Button;
            Assert.That(addButton, Is.Not.Null, "Add button not found.");
            addButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            messageCloser.Join();
            var listView = _window.FindName("itemsControl") as ListView;
            Assert.That(listView, Is.Not.Null, "ListView not found.");

            // Check if the new description exists in the ListView
            bool found = false;
            foreach (var obj in listView.Items)
            {
                dynamic item = obj;
                if (item.Description == newDescription)
                {
                    found = true;
                    break;
                }
            }
            Assert.That(found, Is.True, $"The new item with description '{newDescription}' should be added to the ListView.");
        }

        [Test]
        public void EditButton_Click_EditsItem()
        {
            // Ensure there is at least one item to edit
            var listView = _window.FindName("itemsControl") as ListView;
            Assert.That(listView, Is.Not.Null, "ListView not found.");
            Assert.That(listView.Items.Count, Is.GreaterThan(0), "No items to edit.");

            // Select the first item and get its original description
            listView.SelectedIndex = 0;
            dynamic item = listView.SelectedItem;
            string originalDescription = item.Description;
            string newDescription = originalDescription + "_Edited";

            // Start a thread to handle the ChangeItem dialog
            var dialogHandler = new Thread(() =>
            {
                AutomationElement editDialog = null;
                for (int i = 0; i < 10 && editDialog == null; i++)
                {
                    editDialog = AutomationElement.RootElement.FindFirst(
                        TreeScope.Children,
                        new PropertyCondition(AutomationElement.NameProperty, "ChangeItem")
                    );
                    if (editDialog == null) Thread.Sleep(200);
                }
                Assert.That(editDialog, Is.Not.Null, "Edit dialog not found.");

                // Find the description TextBox by AutomationId
                var descBox = editDialog.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "Description"));
                Assert.That(descBox, Is.Not.Null, "Description TextBox not found.");

                // Set the new value
                var valuePattern = descBox.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
                Assert.That(valuePattern, Is.Not.Null, "ValuePattern not found on DescriptionBox.");
                valuePattern.SetValue(newDescription);

                // Find and click the _Submit button
                var submitButton = editDialog.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "_Submit"));
                Assert.That(submitButton, Is.Not.Null, "_Submit button not found.");
                var invoke = submitButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                Assert.That(invoke, Is.Not.Null, "Invoke pattern not found on _Submit button.");
                invoke.Invoke();
            });
            dialogHandler.Start();

            // Click the Edit button
            var editButton = _window.FindName("edit") as Button;
            Assert.That(editButton, Is.Not.Null, "Edit button not found.");
            editButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            dialogHandler.Join();

            // Validate the item was edited
            Assert.That(item.Description, Is.EqualTo(newDescription), "Item description was not updated.");
        }
    }
}
