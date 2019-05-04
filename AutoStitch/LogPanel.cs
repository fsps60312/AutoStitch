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
        public static void Log(MyImageD img)
        {
            if (!instance.Dispatcher.CheckAccess()) instance.Dispatcher.Invoke(() => instance._Log(img));
            else instance._Log(img);
        }
        void pop_children() { if (stackPanel.Children.Count > 1000) stackPanel.Children.RemoveAt(0); }
        void _Log(MyImageD img)
        {
            pop_children();
            stackPanel.Children.Add(new Image { MaxHeight = 300, Source = img.ToImage().ToBitmapSource(), HorizontalAlignment = System.Windows.HorizontalAlignment.Left });
            scrollViewer.ScrollToBottom();
        }
        void _Log(string text)
        {
            pop_children();
            stackPanel.Children.Add(new Label { Content = text });
            scrollViewer.ScrollToBottom();
        }
    }
}
