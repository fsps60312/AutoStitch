using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.PointsProviders
{
    class SubpixelRefinement : PointsProvider
    {
        IPointsProvider points_provider;
        IMatrixProvider mat_provider;
        public SubpixelRefinement(IPointsProvider points_provider, IMatrixProvider mat_provider)
        {
            this.points_provider = points_provider;
            this.mat_provider = mat_provider;
        }
        public override void Reset()
        {
            base.Reset();
            points_provider.Reset();
            mat_provider.Reset();
        }
        protected override List<ImagePoint> GetPointsInternal()
        {
            double[,] data = mat_provider.GetMatrix().data;
            List<ImagePoint>
                points = points_provider.GetPoints(),
                ans = new List<ImagePoint>();
            foreach(var p in points)
            {
                int x0 = (int)p.x, y0 = (int)p.y;
                int xn = Math.Max(0, x0 - 1), yn = Math.Max(0, y0 - 1);
                int xp = Math.Min(data.GetLength(1) - 1, x0 + 1), yp = Math.Min(data.GetLength(0) - 1, y0 + 1);
                double
                    fnn = data[yn, xn],
                    fn0 = data[y0, xn],
                    fnp = data[yp, xn],
                    f0n = data[yn, x0],
                    f00 = data[y0, x0],
                    f0p = data[yp, x0],
                    fpn = data[yn, xp],
                    fp0 = data[y0, xp],
                    fpp = data[yp, xp];
                double
                    dfx = (fp0 - fn0) / 2,
                    dfy = (f0p - f0n) / 2,
                    dfxx = fp0 - 2 * f00 + fn0,
                    dfyy = f0p - 2 * f00 + f0n,
                    dfxy = (fnn - fnp - fpn + fpp) / 4;
                // -[[dfxx,dfxy],[dfxy,dfyy]]^-1[[dfx],[dfy]]
                double det = dfxx * dfyy - dfxy * dfxy;
                // -1/det[[dfyy,-dfxy],[-dfxy,dfxx]][[dfx],[dfy]]
                double
                    dx = -(dfyy * dfx + (-dfxy) * dfy) / det,
                    dy = -((-dfxy) * dfx + dfxx * dfy) / det;
                //if (Math.Sqrt(dx * dx + dy * dy) < 0.5) dx = dy = 0;
                double x = x0 + dx, y = y0 + dy;
                ans.Add(new ImagePoint(x, y, p.importance + dx * dfx + dy * dfy + 0.5 * (dx * dx * dfxx + 2 * dx * dy * dfxy + dy * dy * dfyy)));
            }
            return ans;
        }
    }
}
