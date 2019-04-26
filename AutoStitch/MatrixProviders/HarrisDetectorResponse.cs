using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.MatrixProviders
{
    class HarrisDetectorResponse : MatrixProvider
    {
        class Detector : MatrixProvider
        {
            IMatrixProvider provider_xx, provider_xy, provider_yy;
            public Detector(IMatrixProvider provider_xx, IMatrixProvider provider_xy, IMatrixProvider provider_yy)
            {
                this.provider_xx = provider_xx;
                this.provider_xy = provider_xy;
                this.provider_yy = provider_yy;
            }
            public override void Reset()
            {
                base.Reset();
                provider_xx.Reset();
                provider_xy.Reset();
                provider_yy.Reset();
            }
            protected override MyMatrix GetMatrixInternal()
            {
                double[,] data_xx = provider_xx.GetMatrix().data;
                double[,] data_xy = provider_xy.GetMatrix().data;
                double[,] data_yy = provider_yy.GetMatrix().data;
                System.Diagnostics.Trace.Assert(Extensions.AllTheSame(data_xx.GetLength(0), data_xy.GetLength(0), data_yy.GetLength(0)));
                System.Diagnostics.Trace.Assert(Extensions.AllTheSame(data_xx.GetLength(1), data_xy.GetLength(1), data_yy.GetLength(1)));
                double[,] data = new double[data_xx.GetLength(0), data_xx.GetLength(1)];
                Parallel.For(0, data.GetLength(0), i =>
                {
                    for (int j = 0; j < data.GetLength(1); j++)
                    {
                        double a00 = data_xx[i, j], a01 = data_xy[i, j], a11 = data_yy[i, j];
                        // Harris
                        //const double k = 0.04;
                        //data[i, j] = (a00 * a11 - a01 * a01) - k * Math.Pow(a00 + a11, 2);
                        // Harmonic mean
                        data[i, j] = (a00 * a11 - a01 * a01) / (a00 + a11);
                    }
                });
                return new MyMatrix(data);
            }
        }
        IMatrixProvider provider;
        public HarrisDetectorResponse(IMatrixProvider provider)
        {
            var mp_ix = new DerivativeX(new GaussianBlurX(provider, 1));
            var mp_iy = new DerivativeY(new GaussianBlurY(provider, 1));
            var mp_sxx = new GaussianBlur(new Dot(mp_ix, mp_ix), 1.5);
            var mp_sxy = new GaussianBlur(new Dot(mp_ix, mp_iy), 1.5);
            var mp_syy = new GaussianBlur(new Dot(mp_iy, mp_iy), 1.5);
            this.provider = new Detector(mp_sxx, mp_sxy, mp_syy);
        }
        public override void Reset()
        {
            base.Reset();
            provider.Reset();
        }
        protected override MyMatrix GetMatrixInternal()
        {
            return provider.GetMatrix();
        }
    }
}
