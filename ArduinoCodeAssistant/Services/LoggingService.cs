using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace ArduinoCodeAssistant.Services
{
    public class LoggingService
    {
        public enum LogLevel
        {
            Completed,
            Info,
            Warning,
            Error
        }

        private readonly RichTextBox _richTextBox;
        private readonly Dispatcher _dispatcher;

        public LoggingService(RichTextBox richTextBox, Dispatcher dispatcher)
        {
            _richTextBox = richTextBox;
            _dispatcher = dispatcher;
        }

        public void Log(string message, LogLevel level = LogLevel.Info, Exception? ex = null)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            _dispatcher.Invoke(() =>
            {
                AppendText(logMessage, GetLogLevelColor(level));
                if (ex != null)
                {
                    AppendText(ex.Message, GetLogLevelColor(LogLevel.Error));
                }
            });
        }

        bool _isRichTextBoxEmpty = true;
        private void AppendText(string text, Color color)
        {
            if (_isRichTextBoxEmpty)
            {
                TextRange textRange = new TextRange(_richTextBox.Document.ContentEnd, _richTextBox.Document.ContentEnd);
                textRange.Text = text;
                textRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
                _isRichTextBoxEmpty = false;
            }
            else
            {
                var run = new Run(text)
                {
                    Foreground = new SolidColorBrush(color)
                };
                var paragraph = new Paragraph(run);
                _richTextBox.Document.Blocks.Add(paragraph);
            }

            _richTextBox.ScrollToEnd();
        }

        private Color GetLogLevelColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Completed => Colors.Green,
                LogLevel.Info => Colors.Black,
                LogLevel.Warning => Colors.Orange,
                LogLevel.Error => Colors.Red,
                _ => Colors.Black,
            };
        }
    }
}
