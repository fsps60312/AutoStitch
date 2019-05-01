using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.ImageD_Providers
{
    class Scale : ImageD_Provider
    {
        IImageD_Provider imgd_provider;
        double scale;
        public Scale(IImageD_Provider imgd_provider,double scale)
        {
            this.imgd_provider = imgd_provider;
            this.scale = scale;
        }
        protected override MyImageD GetImageDInternal()
        {
            MyImageD imgd = imgd_provider.GetImageD();
            int width = (int)Math.Ceiling(imgd.width * scale), height = (int)Math.Ceiling(imgd.height * scale);
            int stride = width * 4;
            MyImageD ans = new MyImageD(new double[height * stride], width, height, stride, imgd.dpi_x, imgd.dpi_y, imgd.format, imgd.palette);
            Parallel.For(0, height, i => {
                for (int j = 0; j < width; j++)
                {//bgra
                    int k = i * stride + j * 4;
                    //x = width > 1 ? i / (width - 1) * (imgd.width - 1) : (imgd.width - 1) / 2;
                    //y = height > 1 ? i / (height - 1) * (imgd.height - 1) : (imgd.height - 1) / 2;
                    //imgd.sample(x, y, out double r, out double g, out double b);
                    imgd.sample(j / scale, i / scale, out double r, out double g, out double b);
                    //System.Diagnostics.Trace.WriteLine($"r={r}, g={g}, b={b}");
                    ans.data[k + 0] = b;
                    ans.data[k + 1] = g;
                    ans.data[k + 2] = r;
                    ans.data[k + 3] = 1;
                }
            });
            //System.Diagnostics.Trace.WriteLine($"width={width}, height={height}");
            return ans;
        }
        public override void Reset()
        {
            base.Reset();
            imgd_provider.Reset();
        }
    }
}
