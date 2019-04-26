using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoStitch
{
    public static class MyImage_Extensions
    {
        #region private methods
        #endregion
        #region translate from MyMatrix
        public static MyImageD ToHeatImageD(this MyMatrix image, double exp)
        {
            double mx = double.MinValue, mn = double.MaxValue;
            for (int i = 0; i < image.data.GetLength(0); i++)
            {
                for (int j = 0; j < image.data.GetLength(1); j++)
                {
                    double v = image.data[i, j];
                    if (v > mx) mx = v;
                    if (v < mn) mn = v;
                }
            }
            LogPanel.Log($"min heat: {mn.ToString("E")}");
            LogPanel.Log($"max heat: {mx.ToString("E")}");
            double[] ans = new double[image.data.Length * 4];
            Parallel.For(0, image.data.GetLength(0), i =>
            {
                for (int j = 0; j < image.data.GetLength(1); j++)
                {
                    double v = image.data[i, j];
                    Utils.GetHeatColor(Math.Pow((v - mn) / (mx - mn), exp), out double r, out double g, out double b);
                    int k = (i * image.data.GetLength(1) + j) * 4;
                    ans[k + 0] = b;
                    ans[k + 1] = g;
                    ans[k + 2] = r;
                    ans[k + 3] = 1;
                }
            });
            return new MyImageD(ans, image.data.GetLength(1), image.data.GetLength(0), image.data.GetLength(1) * 4, 300, 300, PixelFormats.Bgra32, null);
        }
        public static MyImageD ToGrayImageD(this MyMatrix image)
        {
            double mx = double.MinValue, mn = double.MaxValue;
            for(int i=0;i< image.data.GetLength(0);i++)
            {
                for (int j = 0; j < image.data.GetLength(1); j++)
                {
                    double v = image.data[i, j];
                    if (v > mx) mx = v;
                    if (v < mn) mn = v;
                }
            }
            LogPanel.Log($"min heat: {mn.ToString("E")}");
            LogPanel.Log($"max heat: {mx.ToString("E")}");
            double[] ans = new double[image.data.Length * 4];
            Parallel.For(0, image.data.GetLength(0), i =>
            {
                for (int j = 0; j < image.data.GetLength(1); j++)
                {
                    double v = image.data[i, j];
                    double scalar = (v - mn) / (mx - mn);
                    int k = (i * image.data.GetLength(1) + j) * 4;
                    ans[k + 0] = scalar;
                    ans[k + 1] = scalar;
                    ans[k + 2] = scalar;
                    ans[k + 3] = 1;
                }
            });
            return new MyImageD(ans, image.data.GetLength(1), image.data.GetLength(0), image.data.GetLength(1) * 4, 300, 300, PixelFormats.Bgra32, null);
        }
        public static MyImage ToHeatImage(this MyMatrix image,double exp) { return image.ToHeatImageD(exp).ToImage(); }
        public static MyImage ToGrayImage(this MyMatrix image) { return image.ToGrayImageD().ToImage(); }
        #endregion
        #region translate from MyImageD
        public static MyMatrix ToMatrix(this MyImageD image)
        {
            double[,] data = new double[image.height, image.width];
            Parallel.For(0, image.height, i =>
            {
                for (int j = 0; j < image.width; j++)
                {
                    int k = i * image.stride + j * 4;
                    data[i, j] = image.data[k + 0] + image.data[k + 1] + image.data[k + 2] + image.data[k + 3];
                }
            });
            return new MyMatrix(data);
        }
        public static MyImage ToImage(this MyImageD image_d)
        {
            return new MyImage(image_d.data.Select(v => Math.Round(v * 255).ClampByte()).ToArray(), image_d);
        }
        #endregion
        #region translate from MyImage
        public static MyMatrix ToMatrix(this MyImage image)
        {
            double[,]data = new double[image.height, image.width];
            Parallel.For(0, image.height, i =>
            {
                for (int j = 0; j < image.width; j++)
                {
                    int k = i * image.stride + j * 4;
                    data[i, j] = image.data[k + 0] + image.data[k + 1] + image.data[k + 2] + image.data[k + 3];
                }
            });
            return new MyMatrix(data);
        }
        public static MyImageD ToImageD(this MyImage image)
        {
            return new MyImageD(image.data.Select(v => (double)v / 255).ToArray(), image);
        }
        #endregion
    }
    public class MyMatrix
    {
        public double[,] data { get; private set; }
        public MyMatrix(double[,] data) { this.data = data; }
        private double add_color(int x, int y,  double ratio)
        {
            if (!(0 <= y && y < data.GetLength(0) && 0 <= x && x < data.GetLength(1))) return 0;
            return ratio * data[y,x];
        }
        public double sample(double x, double y)
        { // bgra
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            double dx = x - xi, dy = y - yi;
            return
                add_color(xi, yi, (1 - dx) * (1 - dy)) + add_color(xi + 1, yi, dx * (1 - dy)) +
                add_color(xi, yi + 1, (1 - dx) * dy) + add_color(xi + 1, yi + 1, dx * dy);
        }
    }
    public class MyImageD
    {
        public double[] data { get; private set; }
        public int height { get; private set; }
        public int width { get; private set; }
        public int stride { get; private set; }
        public double dpi_x { get; private set; }
        public double dpi_y { get; private set; }
        public PixelFormat format { get; private set; }
        public BitmapPalette palette { get; private set; }
        public MyImageD(double[] _data, MyImage template)
        {
            data = _data;
            height = template.height; width = template.width;
            stride = template.stride;
            dpi_x = template.dpi_x; dpi_y = template.dpi_y;
            format = template.format;
            palette = template.palette;
        }
        public MyImageD(double[] _data, MyImageD template)
        {
            data = _data;
            height = template.height; width = template.width;
            stride = template.stride;
            dpi_x = template.dpi_x; dpi_y = template.dpi_y;
            format = template.format;
            palette = template.palette;
        }
        public MyImageD(MyImageD image) : this((double[])image.data.Clone(), image) { }
        public MyImageD(double[] _data, int _width, int _height, int _stride, double _dpi_x, double _dpi_y, PixelFormat _format, BitmapPalette _palette)
        {
            data = _data;
            height = _height;
            width = _width;
            stride = _stride;
            dpi_x = _dpi_x;
            dpi_y = _dpi_y;
            format = _format;
            palette = _palette;
        }
        private bool add_color(int x, int y, ref double r, ref double g, ref double b, double ratio)
        {
            if (ratio == 0) return true;
            if (!(0 <= y && y < height && 0 <= x && x < width)) return false;
            int k = y * stride + x * 4;
            r += ratio * data[k + 2];
            g += ratio * data[k + 1];
            b += ratio * data[k + 0];
            return true;
        }
        public bool sample(double x, double y, out double r, out double g, out double b)
        { // bgra
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            double dx = x - xi, dy = y - yi;
            //System.Diagnostics.Trace.Assert(xi <= x && x <= xi + 1 && yi <= y && y <= yi + 1);
            r = g = b = 0;
            return
                add_color(xi, yi, ref r, ref g, ref b, (1 - dx) * (1 - dy)) &
                add_color(xi + 1, yi, ref r, ref g, ref b, dx * (1 - dy)) &
                add_color(xi, yi + 1, ref r, ref g, ref b, (1 - dx) * dy) &
                add_color(xi + 1, yi + 1, ref r, ref g, ref b, dx * dy);
        }
    }
    public class MyImage
    {
        public byte[] data { get; private set; }
        public int height { get; private set; }
        public int width { get; private set; }
        public int stride { get; private set; }
        public double dpi_x { get; private set; }
        public double dpi_y { get; private set; }
        public PixelFormat format { get; private set; }
        public BitmapPalette palette { get; private set; }
        public MyImage(string image_name)
        {
            var image = new BitmapImage(new Uri(image_name));
            height = image.PixelHeight; width = image.PixelWidth;
            stride = width * 4;
            dpi_x = image.DpiX; dpi_y = image.DpiY;
            data = new byte[height * stride];
            format = image.Format;
            palette = image.Palette;
            image.CopyPixels(data, stride, 0);
            LogPanel.Log($"height: {height}, width: {width}, stride: {stride}, dpi_x: {dpi_x}, dpi_y: {dpi_y}, format: {format}, palette: {palette}");
        }
        public MyImage(byte[] _data, MyImage template)
        {
            data = _data;
            height = template.height; width = template.width;
            stride = template.stride;
            dpi_x = template.dpi_x; dpi_y = template.dpi_y;
            format = template.format;
            palette = template.palette;
        }
        public MyImage(byte[] _data, MyImageD template)
        {
            data = _data;
            height = template.height; width = template.width;
            stride = template.stride;
            dpi_x = template.dpi_x; dpi_y = template.dpi_y;
            format = template.format;
            palette = template.palette;
        }
        public MyImage(byte[]_data, int _width, int _height,int _stride,double _dpi_x,double _dpi_y,PixelFormat _format,BitmapPalette _palette)
        {
            data = _data;
            height = _height;
            width = _width;
            stride = _stride;
            dpi_x = _dpi_x;
            dpi_y = _dpi_y;
            format = _format;
            palette = _palette;
        }
        public BitmapSource ToBitmapSource()
        {
            return BitmapSource.Create(width, height,
                    dpi_x, dpi_y,
                    format, palette,
                    data, stride);
        }
    }
}
