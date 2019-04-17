using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.MatrixProviders
{
    class DerivativeY : MatrixProvider
    {
        IMatrixProvider provider;
        public DerivativeY(IMatrixProvider provider)
        {
            this.provider = provider;
        }
        public override void Reset()
        {
            base.Reset();
            provider.Reset();
        }
        protected override MyMatrix GetMatrixInternal()
        {
            double[,] raw = provider.GetMatrix().data;
            double[,] data = new double[raw.GetLength(0), raw.GetLength(1)];
            for (int i = 0; i < data.GetLength(0); i++)
            {
                int i1 = Math.Max(0, i - 1), i2 = Math.Min(data.GetLength(0) - 1, i + 1);
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    data[i, j] = raw[i2, j] - raw[i1, j];
                }
            }
            return new MyMatrix(data);
        }
    }
}
