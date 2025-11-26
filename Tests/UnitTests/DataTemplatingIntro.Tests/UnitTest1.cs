using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DataTemplatingIntro;
using NUnit.Framework;

namespace DataTemplatingIntro.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)] // Ensure WPF objects are created on STA
    public class Tests
    {
        [SetUp]
        public void SetUp()
        {
            // Create an Application if one does not exist (safe in test host)
            if (Application.Current == null)
            {
                new Application();
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Do not call Application.Current.Shutdown() - other tests may rely on the Application.
            // Clear MainWindow to avoid cross-test state.
            if (Application.Current != null)
            {
                Application.Current.MainWindow = null;
            }
        }

        [Test]
        public void MyTaskTemplate_IsSelected_ForNonPriorityOneTask_And_ShowsBoundValues()
        {
            // Arrange - create the window so its resources (templates/selectors/tasks) are loaded
            var window = new MainWindow();
            Application.Current.MainWindow = window;

            // Get resources from the window
            var selector = window.FindResource("MyDataTemplateSelector") as TaskListDataTemplateSelector;
          
            Assert.That(selector, Is.Not.Null, "MyDataTemplateSelector resource must exist.");

            var tasks = window.FindResource("MyTodoList") as Tasks;
           
            Assert.That(tasks, Is.Not.Null, "MyTodoList resource must exist.");

            // Pick a task that uses MyTaskTemplate (priority != 1). The Tasks ctor uses "Groceries" as first item.
            var task = tasks.First(t => t.Priority != 1);
            
            // Act - select the DataTemplate and materialize its visual tree
            var selectedTemplate = selector.SelectTemplate(task, window);
            Assert.That(selectedTemplate, Is.Not.Null, "Selector should return a template for a Task instance.");

            // Verify selector returned the MyTaskTemplate resource specifically
            var expectedTemplate = window.FindResource("MyTaskTemplate") as DataTemplate;
        
            Assert.That(selectedTemplate, Is.SameAs(expectedTemplate), "Selector must return MyTaskTemplate for non-priority-1 tasks.");

            // Load the template content and set DataContext so bindings resolve
            var content = selectedTemplate.LoadContent() as FrameworkElement;
          
            Assert.That(content, Is.Not.Null, "Loaded template content must be a FrameworkElement.");

            content.DataContext = task;

            // Allow layout/binding to update (synchronous on same thread, but ensure template is measured/arranged)
            content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            content.Arrange(new Rect(content.DesiredSize));
            Thread.Sleep(10000);
            // Find TextBlocks and assert that bound values are present
            var textBlocks = FindVisualChildren<TextBlock>(content).ToList();
            Thread.Sleep(10000);
            // Collect actual strings once
            var actualTexts = textBlocks.Select(x => x.Text).ToList();
            //if(actualTexts.Contains(null))
            //{
                
            //}

            Thread.Sleep(10000);
            // Assert TaskName is present
            Assert.That(
                actualTexts,
                Does.Contain(task.TaskName),
                $"Expected a TextBlock with text == '{task.TaskName}'. Actual texts: [{string.Join(", ", actualTexts)}]"
            );

            Assert.That(
                actualTexts,
                Does.Contain(task.Description),
                $"Expected a TextBlock with text == '{task.Description}'. Actual texts: [{string.Join(", ", actualTexts)}]"
            );

            Assert.That( actualTexts, Does.Contain(task.Priority.ToString()),
                $"Expected a TextBlock with text == '{task.Priority}'. Actual texts: [{string.Join(", ", actualTexts)}]"
            );

            //// Assert that TaskName, Description and Priority appear somewhere in the visual tree
            //Assert.That(textBlocks.Any(tb => tb.Text == task.TaskName), Is.True, $"Expected a TextBlock with TaskName '{task.TaskName}' in the template. but was '{tb.Text}'");
            //Assert.That(textBlocks.Any(tb => tb.Text == task.Description), Is.True,
            //    $"Expected a TextBlock with Description '{task.Description}' in the template.");
            //Assert.That(textBlocks.Any(tb => tb.Text == task.Priority.ToString()), Is.True,
            //    $"Expected a TextBlock with Priority '{task.Priority}' in the template.");
        }

        // Helper: walk visual tree of the loaded template
        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;

                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}