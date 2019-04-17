using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.PointsProviders
{
    class LocalMaximum : PointsProvider
    {
        IMatrixProvider provider;
        double threshold;
        public LocalMaximum(IMatrixProvider provider,double threshold)
        {
            this.provider = provider;
            this.threshold = threshold;
        }
        public override void Reset()
        {
            base.Reset();
            provider.Reset();
        }
        protected override List<ImagePoint> GetPointsInternal()
        {
            var mat = provider.GetMatrix().data;
            List<ImagePoint> ans = new List<ImagePoint>();
            for(int i = 1; i + 1 < mat.GetLength(0); i++)
            {
                for(int j = 1; j + 1 < mat.GetLength(1); j++)
                {
                    double v = mat[i, j];
                    if (v >= threshold)
                    {
                        //System.Diagnostics.Trace.WriteLine($"pass threshold: {i},{j}");
                        if(v>mat[i-1,j-1]&&v>mat[i-1,j]&&v>mat[i-1,j+1]&&
                            v>mat[i,j-1]&&v>mat[i,j+1]&&
                            v>mat[i+1,j-1]&&v>mat[i+1,j]&&v>mat[i+1,j+1])
                        {
                            ans.Add(new ImagePoint(j, i, v));
                        }
                    }
                }
            }
            ans.Sort();
            return ans;
        }
    }
}
