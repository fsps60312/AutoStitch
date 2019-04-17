using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.MatrixProviders
{
    class Dot:MatrixProvider
    {
        IMatrixProvider provider1,provider2;
        public Dot(IMatrixProvider provider1,IMatrixProvider provider2)
        {
            this.provider1 = provider1;
            this.provider2 = provider2;
        }
        public override void Reset()
        {
            base.Reset();
            provider1.Reset();
            provider2.Reset();
        }
        protected override MyMatrix GetMatrixInternal()
        {
            double[,] data1 = provider1.GetMatrix().data;
            double[,] data2 = provider2.GetMatrix().data;
            System.Diagnostics.Trace.Assert(data1.GetLength(0) == data2.GetLength(0) && data1.GetLength(1) == data2.GetLength(1));
            double[,] data = new double[data1.GetLength(0), data1.GetLength(1)];
            for (int i = 0; i < data.GetLength(0); i++) for (int j = 0; j < data.GetLength(1); j++) data[i, j] = data1[i, j] * data2[i, j];
            return new MyMatrix(data);
        }
    }
}
