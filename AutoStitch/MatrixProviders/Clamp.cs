﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.MatrixProviders
{
    class Clamp : MatrixProvider
    {
        MatrixProvider provider;
        double mn, mx;
        public Clamp(MatrixProvider provider,double mn,double mx)
        {
            this.provider = provider;
            this.mn = mn;
            this.mx = mx;
        }
        public override void Reset()
        {
            base.Reset();
            provider.Reset();
        }
        protected override MyMatrix GetMatrixInternal()
        {
            double[,] data = (double[,])provider.GetMatrix().data.Clone();
            for (int i = 0; i < data.GetLength(0); i++) for (int j = 0; j < data.GetLength(1); j++) data[i, j] = Math.Max(mn, Math.Min(mx, data[i, j]));
            return new MyMatrix(data);
        }
    }
}
