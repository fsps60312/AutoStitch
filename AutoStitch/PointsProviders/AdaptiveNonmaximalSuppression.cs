using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.PointsProviders
{
    class AdaptiveNonmaximalSuppression : PointsProvider
    {
        IPointsProvider provider;
        int max_points_to_keep;
        public AdaptiveNonmaximalSuppression(IPointsProvider provider,int max_points_to_keep)
        {
            this.provider = provider;
            this.max_points_to_keep = max_points_to_keep;
        }
        public override void Reset()
        {
            base.Reset();
            provider.Reset();
        }
        private List<ImagePoint> eliminate_with_radius(List<ImagePoint>points,double radius)
        {
            HashSet<ImagePoint> ps = new HashSet<ImagePoint>(points);
            List<ImagePoint> ans = new List<ImagePoint>();
            while(ps.Count>0)
            {
                var p = ps.Max();
                ans.Add(p);
                List<ImagePoint> to_remove = new List<ImagePoint>();
                foreach(var q in ps) if (Math.Pow(p.x - q.x, 2) + Math.Pow(p.y - q.y, 2) < radius * radius) to_remove.Add(q);
                foreach (var q in to_remove) ps.Remove(q);
            }
            return ans;
        }
        protected override List<ImagePoint> GetPointsInternal()
        {
            List<ImagePoint> ps = provider.GetPoints();
            double l = 0, r = Math.Sqrt(Math.Pow(ps.Max(p => p.x), 2) + Math.Pow(ps.Max(p => p.y), 2));
            while(r-l>1e-9)
            {
                double mid = (l + r) / 2;
                if (eliminate_with_radius(ps, mid).Count > max_points_to_keep) l = mid;
                else r = mid;
            }
            return eliminate_with_radius(ps, r); // guaranteed that the count <= max_points_to_keep
        }
    }
}
