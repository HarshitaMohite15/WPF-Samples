using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using TableRows;

namespace TableRows.Tests
{
    [TestFixture]
    public class UnitTest1
    {
        [Test, Apartment(ApartmentState.STA)]
        public void AddRow_Then_FindRowInTrg1_ByCellText()
        {
            // Arrange - create and show window so name scope is available
            var window = new MainWindow();
            window.Show();

            var trg = window.FindName("trg1") as TableRowGroup;
            Assert.That(trg, Is.Not.Null, "TableRowGroup 'trg1' not found");

            var before = trg.Rows.Count;

            // Act - simulate button click
            var addRowButton = (Button)window.FindName("btnAddRow");
            addRowButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            // Allow WPF to process layout/dispatch if needed
            Thread.Sleep(50);

            // Assert - row count increased
            Assert.That(trg.Rows.Count, Is.EqualTo(before + 1), "Row count did not increase");

            // Find the newly added row by expected text
            var expected = "A new Row and Cell have been Added to the Table";
            var foundRow = FindRowByCellText(trg, expected);

            Assert.That(foundRow, Is.Not.Null, $"No TableRow found containing text '{expected}'");
            // Thread.Sleep(5000);
            // Verify the cell text exactly
            var cellText = GetFirstCellText(foundRow!);
            Assert.That(cellText, Is.EqualTo(expected));

            window.Close();
        }

        // Helper: returns the first TableRow that contains the specified text in any TableCell's Paragraph Run
        private static TableRow? FindRowByCellText(TableRowGroup trg, string text)
        {
            foreach (var row in trg.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    foreach (var block in cell.Blocks.OfType<Paragraph>())
                    {
                        var run = block.Inlines.FirstInline as Run;
                        if (run != null && run.Text != null && run.Text.Contains(text))
                            return row;
                    }
                }
            }
            return null;
        }

        // Helper: get first cell text for quick assertions
        private static string? GetFirstCellText(TableRow row)
        {
            var cell = row.Cells.FirstOrDefault();
            if (cell == null) return null;
            var para = cell.Blocks.OfType<Paragraph>().FirstOrDefault();
            var run = para?.Inlines.FirstInline as Run;
            return run?.Text;
        }
    }
}