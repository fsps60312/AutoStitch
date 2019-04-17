using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.MatrixProviders
{
    class Add:MatrixProvider
    {
        IMatrixProvider provider0;
        IMatrixProvider[] providers;
        public Add(IMatrixProvider provider0, params IMatrixProvider[] providers)
        {
            this.provider0 = provider0;
            this.providers = providers;
        }
        public override void Reset()
        {
            base.Reset();
            provider0.Reset();
            foreach (var provider in providers) provider.Reset();
        }
        protected override MyMatrix GetMatrixInternal()
        {
            double[,] data = (double[,])provider0.GetMatrix().data.Clone();
            foreach(var provider in providers)
            {
                var addi = provider.GetMatrix().data;
                System.Diagnostics.Trace.Assert(addi.GetLength(0) == data.GetLength(0) && addi.GetLength(1) == data.GetLength(1));
                for(int i=0;i<data.GetLength(0);i++)
                {
                    for(int j=0;j<data.GetLength(1);j++)
                    {
                        data[i, j] += addi[i, j];
                    }
                }
            }
            return new MyMatrix(data);
        }
    }
}
