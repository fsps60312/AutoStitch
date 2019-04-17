using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.ImageD_Providers
{
    class GrayImageD : ImageD_Provider
    {
        MatrixProvider provider;
        public GrayImageD(MatrixProvider provider)
        {
            this.provider = provider;
        }
        protected override MyImageD GetImageDInternal()
        {
            return provider.GetMatrix().ToGrayImageD();
        }
        public override void Reset()
        {
            base.Reset();
            provider.Reset();
        }
    }
}
