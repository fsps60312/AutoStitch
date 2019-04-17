using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace AutoStitch
{
    class SourceImagePanel : ContentControl,IImagesProvider,IImageProvider,IImageD_Provider
    {
        StackPanel stackPanel;
        public SourceImagePanel() { InitializeViews(); }
        void InitializeViews()
        {
            this.MinHeight = 200;
            this.MaxHeight = 300;
            this.Content =new Grid
            {
                ColumnDefinitions=
                {
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto)},
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)}
                },
                Children =
                {
                    new Button
                    {
                        Content="Open"
                    }.Set(()=>OpenImages()).Set(0,0),
                    new ScrollViewer
                    {
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        Content = (stackPanel = new StackPanel { Orientation = Orientation.Horizontal })
                    }.Set(0,1)
                }
            };
        }
        void ShowImages()
        {
            var add_image = new Action<MyImage>(img =>
            {
                stackPanel.Children.Add(new Grid
                {
                    Margin = new Thickness(2),
                    RowDefinitions =
                      {
                        new RowDefinition{Height=new GridLength(1,GridUnitType.Star)}
                      },
                    Children =
                      {
                        new Image{Source=img.ToBitmapSource()}.Set(0,0)
                      }
                });
            });
            stackPanel.Children.Clear();
            foreach (var img in images) add_image(img);
        }
        List<MyImage> images = null;

        public event ImageChangedEventHandler ImageChanged;
        public event ImageDChangedEventHandler ImageDChanged;
        public List<MyImage> GetImages()
        {
            if (images == null) LogPanel.Log("Warning: [SourceImagePanel] images == null");
            return images;
        }
        public void Reset() { }
        public MyImage GetImage()
        {
            if (images == null) { LogPanel.Log("Warning: [SourceImagePanel] images == null"); return null; }
            if (images.Count <= 0) { LogPanel.Log($"Warning: [SourceImagePanel] images.Count == {images.Count}"); return null; }
            return images[0];
        }
        public MyImageD GetImageD()
        {
            return GetImage()?.ToImageD();
        }
        public void OpenImages()
        {
            LogPanel.Log("Opening images...");
            var dialog = new OpenFileDialog { Multiselect = true };
            if (dialog.ShowDialog() == true)
            {
                var ans = dialog.FileNames;
                if (ans != null)
                {
                    images = new List<MyImage>();
                    foreach (var f in ans)
                    {
                        LogPanel.Log(f);
                        images.Add(new MyImage(f));
                    }
                    LogPanel.Log($"{ans.Length} files selected.");
                    ImageChanged?.Invoke(GetImage());
                    ShowImages();
                }
                else LogPanel.Log("No files selected.");
            }
            else LogPanel.Log("Canceled.");
        }
    }
}
