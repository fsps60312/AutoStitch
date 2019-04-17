using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.ImageD_Providers
{
    class HeatImageD : ImageD_Provider
    {
        IMatrixProvider provider;
        double exp;
        public HeatImageD(IMatrixProvider provider,double exp=1)
        {
            this.provider = provider;
            this.exp = exp;
        }
        protected override MyImageD GetImageDInternal()
        {
            return provider.GetMatrix().ToHeatImageD(exp);
        }
        public override void Reset()
        {
            base.Reset();
            provider.Reset();
        }
    }
}
