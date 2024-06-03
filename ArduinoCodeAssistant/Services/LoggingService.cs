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

        private readonly RichTextBox _logTextBox;
        private readonly RichTextBox _serialTextBox;
        private readonly Dispatcher _dispatcher;

        public LoggingService(RichTextBox logTextBox, RichTextBox serialTextBox, Dispatcher dispatcher)
        {
            _logTextBox = logTextBox;
            _serialTextBox = serialTextBox;
            _dispatcher = dispatcher;
        }

        public void Log(string message, LogLevel level = LogLevel.Info, Exception? ex = null)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            _dispatcher.Invoke(() =>
            {
                AppendText(_logTextBox, logMessage, GetLogLevelColor(level));
                if (ex != null)
                {
                    AppendText(_logTextBox, ex.Message, GetLogLevelColor(LogLevel.Error));
                }
            });
        }

        public void SerialLog(string message)
        {
            _dispatcher.Invoke(() =>
            {
                AppendText(_serialTextBox, message, GetLogLevelColor(LogLevel.Info));
            });
        }

        public void ClearLogTextBox()
        {
            _dispatcher.Invoke(() =>
            {
                ClearText(_logTextBox);
            });
        }

        public void ClearSerialTextBox()
        {
            _dispatcher.Invoke(() =>
            {
                ClearText(_serialTextBox);
            });
        }

        private void AppendText(RichTextBox richTextBox, string text, Color color)
        {
            if (string.IsNullOrWhiteSpace(new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text))
            {
                TextRange textRange = new TextRange(richTextBox.Document.ContentEnd, richTextBox.Document.ContentEnd);
                textRange.Text = text;
                textRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            }
            else
            {
                var run = new Run(text)
                {
                    Foreground = new SolidColorBrush(color)
                };
                var paragraph = new Paragraph(run);
                richTextBox.Document.Blocks.Add(paragraph);
            }
        }

        private void ClearText(RichTextBox richTextBox)
        {
            richTextBox.Document.Blocks.Clear();
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
