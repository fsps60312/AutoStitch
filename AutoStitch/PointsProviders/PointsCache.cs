using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.PointsProviders
{
    class PointsCache:PointsProvider
    {
        Func<List<ImagePoint>> get_points;
        public PointsCache(List<ImagePoint> points) { this.get_points = () => points; }
        protected override List<ImagePoint> GetPointsInternal() { return get_points(); }
    }
}
