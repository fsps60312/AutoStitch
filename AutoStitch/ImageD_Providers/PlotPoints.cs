using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.ImageD_Providers
{
    class PlotPoints : ImageD_Provider
    {
        IImageD_Provider imgd_provider;
        IPointsProvider points_provider;
        double exp;
        public PlotPoints(IImageD_Provider imgd_provider, IPointsProvider points_provider, double exp = 1)
        {
            this.imgd_provider = imgd_provider;
            this.points_provider = points_provider;
            this.exp = exp;
        }
        private bool splat_pixel(MyImageD img, int x, int y, double r, double g, double b, double ratio)
        { // bgra
            if (!(0 <= x && x < img.width && 0 <= y && y < img.height)) return false;
            int k = y * img.stride + x * 4;
            if(img.data[k+3]>1- ratio)
            {
                double t = (1 - ratio) / img.data[k + 3];
                img.data[k + 0] *= t;
                img.data[k + 1] *= t;
                img.data[k + 2] *= t;
                img.data[k + 3] = 1 - ratio;
            }
            img.data[k + 0] += b * ratio;
            img.data[k + 1] += g * ratio;
            img.data[k + 2] += r * ratio;
            img.data[k + 3] += ratio;
            return true;
        }
        private bool plot_dot(MyImageD img, double x, double y, double r, double g, double b)
        { // bgra
            if (!(0 <= x && x <= img.width - 1 && 0 <= y && y <= img.height - 1)) return false;
            int xi = (int)x, yi = (int)y;
            double dx = x - xi, dy = y - yi;
            splat_pixel(img, xi + 0, yi + 0, r, g, b, (1 - dx) * (1 - dy));
            splat_pixel(img, xi + 0, yi + 1, r, g, b, (1 - dx) * dy);
            splat_pixel(img, xi + 1, yi + 0, r, g, b, dx * (1 - dy));
            splat_pixel(img, xi + 1, yi + 1, r, g, b, dx * dy);
            return true;
        }
        private void plot_cross(MyImageD img, double x, double y, double r, double g, double b)
        {
            //r = g = b = 1;
            const int cross_length = 3;
            if (!plot_dot(img, x, y, r, g, b)) return;
            for (int d = 1; d <= cross_length && plot_dot(img, x, y - d, r, g, b); d++) ;
            for (int d = 1; d <= cross_length && plot_dot(img, x, y + d, r, g, b); d++) ;
            for (int d = 1; d <= cross_length && plot_dot(img, x - d, y, r, g, b); d++) ;
            for (int d = 1; d <= cross_length && plot_dot(img, x + d, y, r, g, b); d++) ;
        }
        protected override MyImageD GetImageDInternal()
        {
            MyImageD imgd = new MyImageD(imgd_provider.GetImageD());
            var points = points_provider.GetPoints();
            LogPanel.Log($"[PlotPoints] {points.Count} points.");
            if (points.Count > 0)
            {
                MyImageD cross_img = new MyImageD(new double[imgd.data.Length], imgd);
                double
                    mn = points.Min(p => p.importance),
                    mx = points.Max(p => p.importance);
                foreach (var p in points.Reverse<ImagePoint>())
                {
                    Utils.GetHeatColor((p.importance - mn) / (mx - mn), out double r, out double g, out double b);
                    plot_cross(cross_img, p.x, p.y, r, g, b);
                }
                Parallel.For(0, imgd.height, i =>
                {
                    for (int j = 0; j < imgd.width; j++)
                    {
                        int k = i * imgd.stride + j * 4;
                        double ratio = cross_img.data[k + 3];
                        imgd.data[k + 0] = (1 - ratio) * imgd.data[k + 0] + cross_img.data[k + 0];
                        imgd.data[k + 1] = (1 - ratio) * imgd.data[k + 1] + cross_img.data[k + 1];
                        imgd.data[k + 2] = (1 - ratio) * imgd.data[k + 2] + cross_img.data[k + 2];
                    }
                });
            }
            return imgd;
        }
        public override void Reset()
        {
            base.Reset();
            imgd_provider.Reset();
            points_provider.Reset();
        }
    }
}
