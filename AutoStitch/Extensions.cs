using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace AutoStitch
{
    public static class Extensions
    {
        public static MyImage RotateClockwise(this MyImage image)
        {
            int width = image.height, height = image.width;
            int stride = width * 4;
            MyImage ans = new MyImage(new byte[width * height * 4], width, height, stride, image.dpi_x, image.dpi_y, image.format, image.palette);
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {//bgra
                    int k = stride * i + j * 4;
                    int _ = image.stride * (width - 1 - j) + i * 4;
                    ans.data[k + 0] = image.data[_ + 0];
                    ans.data[k + 1] = image.data[_ + 1];
                    ans.data[k + 2] = image.data[_ + 2];
                    ans.data[k + 3] = image.data[_ + 3];
                }
            }
            return ans;
        }
        public static double Clamp(this double v,double mn,double mx) { return Math.Max(mn, Math.Min(mx, v)); }
        public static byte ClampByte(this double v)
        {
            return (byte)Math.Max(0, Math.Min(255, v));
        }
        public static void Save(this BitmapSource image, Stream stream)
        {
            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(stream);
        }
        public static Button Set(this Button button, Action action)
        {
            button.Click += delegate { action(); };
            return button;
        }
        public static UIElement Set(this UIElement uIElement, int row, int column)
        {
            Grid.SetRow(uIElement, row);
            Grid.SetColumn(uIElement, column);
            return uIElement;
        }
        public static UIElement SetSpan(this UIElement uIElement, int rowSpan, int columnSpan)
        {
            Grid.SetRowSpan(uIElement, rowSpan);
            Grid.SetColumnSpan(uIElement, columnSpan);
            return uIElement;
        }
    }
}
