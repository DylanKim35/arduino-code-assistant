using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace ArduinoCodeAssistant.Helpers
{
    public static class ScrollViewerExtensions
    {
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(ScrollViewerExtensions), new PropertyMetadata(false, OnAutoScrollChanged));

        public static bool GetAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollProperty, value);
        }

        private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBox richTextBox)
            {
                if ((bool)e.NewValue)
                {
                    richTextBox.TextChanged += RichTextBox_TextChanged;
                }
                else
                {
                    richTextBox.TextChanged -= RichTextBox_TextChanged;
                }
            }
        }

        private static void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is RichTextBox richTextBox)
            {
                var scrollViewer = GetScrollViewer(richTextBox);
                if (scrollViewer != null)
                {
                    bool isAtBottom = scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight;

                    if (isAtBottom)
                    {
                        scrollViewer.ScrollToEnd();
                    }
                }
            }
        }

        private static ScrollViewer? GetScrollViewer(DependencyObject dependencyObject)
        {
            if (dependencyObject is ScrollViewer)
                return (ScrollViewer)dependencyObject;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
            {
                var child = VisualTreeHelper.GetChild(dependencyObject, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
