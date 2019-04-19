using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.PointsProviders
{
    public class MSOP_DescriptorVector:PointsProvider
    {
        public class Descriptor
        {
            public double[,] v { get; private set; }
            public Descriptor(double[,] v) { this.v = v; }
            public double difference(Descriptor descriptor)
            {
                double ans = 0;
                System.Diagnostics.Trace.Assert(v.GetLength(0) == descriptor.v.GetLength(0) && v.GetLength(1) == descriptor.v.GetLength(1));
                for (int i = 0; i < v.GetLength(0); i++)
                {
                    for (int j = 0; j < v.GetLength(1); j++)
                    {
                        ans += Math.Pow(v[i, j] - descriptor.v[i, j], 2);
                    }
                }
                return Math.Sqrt(ans);
            }
        }
        PointsProvider points_provider;
        MatrixProvider mat_provider, dx_provider,dy_provider;
        public MSOP_DescriptorVector(PointsProvider points_provider,MatrixProvider gray_image_provider)
        {
            this.points_provider = points_provider;
            this.mat_provider = gray_image_provider;
            MatrixProvider blur_provider = new MatrixProviders.GaussianBlur(gray_image_provider, 4.5);
            this.dx_provider = new MatrixProviders.DerivativeX(blur_provider);
            this.dy_provider = new MatrixProviders.DerivativeY(blur_provider);
        }
        protected override List<ImagePoint> GetPointsInternal()
        {
            MyMatrix image = mat_provider.GetMatrix(), dx_mat = dx_provider.GetMatrix(), dy_mat = dy_provider.GetMatrix();
            List<ImagePoint> ans = new List<ImagePoint>();
            foreach(ImagePoint p in points_provider.GetPoints())
            {
                double
                    ox = dx_mat.sample(p.x, p.y),
                    oy = dy_mat.sample(p.x, p.y);
                double len = Math.Sqrt(ox * ox + oy * oy);
                ox /= len; oy /= len;
                double[] vec_flat = new double[8* 8];
                for (int i = 0; i < 40; i++)
                {
                    for (int j = 0; j < 40; j++)
                    {
                        // x: oy-ox
                        // y: -ox-oy
                        vec_flat[i / 5 * 8 + j / 5] += image.sample(p.x + (j - 19.5) * (oy - ox), p.y + (i - 19.5) * (-ox - oy));
                    }
                }
                double mu = vec_flat.Sum() / vec_flat.Length;
                double ro = Math.Sqrt(vec_flat.Sum(v => v * v) / vec_flat.Length - mu * mu);
                vec_flat = vec_flat.Select(v => (v - mu) / ro).ToArray();
                double[,] vec = new double[8, 8];
                for (int i = 0; i < 64; i++) vec[i / 8, i % 8] = vec_flat[i];
                ans.Add(new ImagePoint(p.x, p.y, p.importance, new Descriptor(vec)));
            }
            return ans;
        }
        public override void Reset()
        {
            base.Reset();
            points_provider.Reset();
            dx_provider.Reset();
            dy_provider.Reset();
        }
    }
}
