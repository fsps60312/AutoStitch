using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AutoStitch
{
    class LogPanel : ContentControl
    {
        static LogPanel instance;
        ScrollViewer scrollViewer;
        StackPanel stackPanel;
        void InitializeViews()
        {
            this.Content = (scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = (stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical
                })
            });
        }
        public LogPanel()
        {
            InitializeViews();
            instance = this;
        }
        public static void Log(string text)
        {
            if (!instance.Dispatcher.CheckAccess()) instance.Dispatcher.Invoke(() => instance._Log(text));
            else instance._Log(text);
        }
        void _Log(string text)
        {
            if (stackPanel.Children.Count > 100) stackPanel.Children.RemoveAt(0);
            stackPanel.Children.Add(new Label { Content = text });
            scrollViewer.ScrollToBottom();
        }
    }
}
