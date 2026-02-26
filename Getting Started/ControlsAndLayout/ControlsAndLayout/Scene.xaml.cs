// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace ControlsAndLayout
{
    public partial class Scene
    {
        public bool RealTimeUpdate = true;

        private void HandleSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (sender == null)
                return;
            var lb = sender as ListBox;
            if (lb == null)
                return;
            // Use the selected item as the Details DataContext so bindings like XPath=@Title
            // resolve against the selected sample node. This also avoids showing the whole
            // collection as the details context.
            if (lb.SelectedItem != null)
                Details.DataContext = lb.SelectedItem;
            // Clear selection in the other ListBox so only the current expander's selection remains.
            if (lb == LayoutListBox)
            {
                ClearListBoxSelection(ControlsListBox);
            }
            else if (lb == ControlsListBox)
            {
                ClearListBoxSelection(LayoutListBox);
            }
        }

        private static void ClearListBoxSelection(ListBox other)
        {
            if (other == null)
                return;

            // Clear CollectionView current item (prevents implicit current item via XmlDataProvider)
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(other.ItemsSource ?? other.Items);
            view?.MoveCurrentToPosition(-1);

            // Clear visual selection
            other.SelectedIndex = -1;
        }
        //to clear the selection when the expander is expanded
        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is not Expander expander)
                return;
            // If the expander's direct content is the ListBox, clear it.
            if (expander.Content is ListBox lb)
            {
                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(lb.ItemsSource ?? lb.Items);
                view?.MoveCurrentToPosition(-1);
                lb.SelectedIndex = -1;
            }
        }
        protected void HandleTextChanged(object sender, TextChangedEventArgs me)
        {
            if (RealTimeUpdate) ParseCurrentBuffer();
        }

        private void ParseCurrentBuffer()
        {
            try
            {
                var ms = new MemoryStream();
                var sw = new StreamWriter(ms);
                var str = TextBox1.Text;
                sw.Write(str);
                sw.Flush();
                ms.Flush();
                ms.Position = 0;
                try
                {
                    var content = XamlReader.Load(ms);
                    if (content != null)
                    {
                        cc.Children.Clear();
                        cc.Children.Add((UIElement) content);
                    }
                    TextBox1.Foreground = Brushes.Black;
                    ErrorText.Text = "";
                }

                catch (XamlParseException xpe)
                {
                    TextBox1.Foreground = Brushes.Red;
                    TextBox1.TextWrapping = TextWrapping.Wrap;
                    ErrorText.Text = xpe.Message;
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        protected void OnClickParseButton(object sender, RoutedEventArgs args)
        {
            ParseCurrentBuffer();
        }

        protected void ShowPreview(object sender, RoutedEventArgs args)
        {
            PreviewRow.Height = new GridLength(1, GridUnitType.Star);
            CodeRow.Height = new GridLength(0);
        }

        protected void ShowCode(object sender, RoutedEventArgs args)
        {
            PreviewRow.Height = new GridLength(0);
            CodeRow.Height = new GridLength(1, GridUnitType.Star);
        }

        protected void ShowSplit(object sender, RoutedEventArgs args)
        {
            PreviewRow.Height = new GridLength(1, GridUnitType.Star);
            CodeRow.Height = new GridLength(1, GridUnitType.Star);
        }
    }
}