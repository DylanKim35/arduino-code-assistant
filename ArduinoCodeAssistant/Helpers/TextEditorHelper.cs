using System.Windows;
using ICSharpCode.AvalonEdit;

namespace ArduinoCodeAssistant.Helpers
{
    public static class TextEditorHelper
    {
        public static readonly DependencyProperty BindableTextProperty =
            DependencyProperty.RegisterAttached(
                "BindableText",
                typeof(string),
                typeof(TextEditorHelper),
                new FrameworkPropertyMetadata(string.Empty, OnBindableTextChanged));

        public static string GetBindableText(DependencyObject obj)
        {
            return (string)obj.GetValue(BindableTextProperty);
        }

        public static void SetBindableText(DependencyObject obj, string value)
        {
            obj.SetValue(BindableTextProperty, value);
        }

        private static void OnBindableTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextEditor editor)
            {
                editor.TextChanged -= OnEditorTextChanged;

                var newText = (string)e.NewValue;
                if (newText != editor.Text)
                {
                    editor.Text = newText;
                }

                editor.TextChanged += OnEditorTextChanged;
            }
        }

        private static void OnEditorTextChanged(object sender, EventArgs e)
        {
            var editor = (TextEditor)sender;
            SetBindableText(editor, editor.Text);
        }
    }
}
