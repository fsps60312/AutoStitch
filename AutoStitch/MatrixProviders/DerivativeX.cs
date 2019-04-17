using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.MatrixProviders
{
    class DerivativeX:MatrixProvider
    {
        IMatrixProvider provider;
        public DerivativeX(IMatrixProvider provider)
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
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    int jl = Math.Max(0, j - 1), jr = Math.Min(data.GetLength(1) - 1, j + 1);
                    data[i, j] = raw[i, jr] - raw[i, jl];
                }
            }
            return new MyMatrix(data);
        }
    }
}
