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
        private bool _suppressSelectionHandling;
        private void HandleSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (_suppressSelectionHandling) return;
            try
            {
                _suppressSelectionHandling = true;
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
                    if (ControlsListBox != null && ControlsListBox.SelectedIndex != -1)
                    {
                        ControlsListBox.SelectedIndex = -1;
                    }
                }
                else if (lb == ControlsListBox)
                {
                    if (LayoutListBox != null && LayoutListBox.SelectedIndex != -1)
                    {
                        LayoutListBox.SelectedIndex = -1;
                    }
                }
            }
            finally { _suppressSelectionHandling = false; }
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