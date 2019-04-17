using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AutoStitch
{
    class ImageViewer:Button
    {
        Image img = new Image();
        private ImageViewer()
        {
            this.MinHeight = 200;
            this.MaxHeight = 400;
            this.Margin = new System.Windows.Thickness(2);
            this.Content = img;
            this.Click += delegate
            {
                var dialog = new Microsoft.Win32.SaveFileDialog();
                if (dialog.ShowDialog() == true)
                {
                    using (var stream = dialog.OpenFile())
                    {
                        (img.Source as System.Windows.Media.Imaging.BitmapSource).Save(stream);
                    }
                    LogPanel.Log($"image saved as {dialog.FileName}");
                }
                else LogPanel.Log("canceled.");
            };
        }
        public ImageViewer(IMatrixProvider provider):this()
        {
            provider.MatrixChanged += (matrix) => {
                if (!this.Dispatcher.CheckAccess()) Dispatcher.Invoke(() => img.Source = matrix.ToHeatImage(1).ToBitmapSource());
                else img.Source = matrix.ToHeatImage(1).ToBitmapSource();
            };
        }
        public ImageViewer(IImageD_Provider provider,bool heatmap=true) : this()
        {
            provider.ImageDChanged += (image) => {
                MyImage i = heatmap ? image.ToMatrix().ToHeatImage(1) : image.ToImage();
                if (!this.Dispatcher.CheckAccess()) Dispatcher.Invoke(() => img.Source = i.ToBitmapSource());
                else img.Source = i.ToBitmapSource();
            };
        }
        public ImageViewer(IImageProvider provider) : this()
        {
            provider.ImageChanged += (image) => {
                if (!this.Dispatcher.CheckAccess()) Dispatcher.Invoke(() => img.Source = image.ToBitmapSource());
                else img.Source = image.ToBitmapSource();
            };
        }
    }
}
