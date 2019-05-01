using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.ImageD_Providers
{
    class GaussianBlur:ImageD_Provider
    {
        IMatrixProvider blue_channel, green_channel, red_channel, alpha_channel;
        double ro;
        public GaussianBlur(IImageD_Provider imaged_provider,double ro)
        {
            this.blue_channel = new MatrixProviders.GaussianBlur(MatrixProviders.Filter.Blue(imaged_provider), ro);
            this.green_channel = new MatrixProviders.GaussianBlur(MatrixProviders.Filter.Green(imaged_provider), ro);
            this.red_channel = new MatrixProviders.GaussianBlur(MatrixProviders.Filter.Red(imaged_provider), ro);
            this.alpha_channel = new MatrixProviders.GaussianBlur(MatrixProviders.Filter.Alpha(imaged_provider), ro);
            this.ro = ro;
        }
        protected override MyImageD GetImageDInternal()
        {
            MyMatrix
                blue_mat = blue_channel.GetMatrix(),
                green_mat = green_channel.GetMatrix(),
                red_mat = red_channel.GetMatrix(),
                alpha_mat = alpha_channel.GetMatrix();
            System.Diagnostics.Trace.Assert(Utils.AllTheSame(blue_mat.data.GetLength(0), green_mat.data.GetLength(0), red_mat.data.GetLength(0), alpha_mat.data.GetLength(0)));
            System.Diagnostics.Trace.Assert(Utils.AllTheSame(blue_mat.data.GetLength(1), green_mat.data.GetLength(1), red_mat.data.GetLength(1), alpha_mat.data.GetLength(1)));
            MyImageD ans = new MyImageD(blue_mat.data.GetLength(1), blue_mat.data.GetLength(0));
            Parallel.For(0, ans.height, i =>
             {
                 for (int j = 0; j < ans.width; j++)
                 {//bgra
                     int k = i * ans.stride + j * 4;
                     ans.data[k + 0] = blue_mat.data[i, j];
                     ans.data[k + 1] = green_mat.data[i, j];
                     ans.data[k + 2] = red_mat.data[i, j];
                     ans.data[k + 3] = alpha_mat.data[i, j];
                 }
             });
            return ans;
        }
        public override void Reset()
        {
            base.Reset();
            blue_channel.Reset();
            green_channel.Reset();
            red_channel.Reset();
            alpha_channel.Reset();
        }
    }
}
