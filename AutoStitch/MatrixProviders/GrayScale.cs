using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.MatrixProviders
{
    class GrayScale:MatrixProvider
    {
        IImageD_Provider provider;
        public GrayScale(IImageD_Provider provider)
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
            return provider.GetImageD().ToMatrix();
        }
    }
}
