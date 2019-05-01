using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.PointsProviders
{
    class MultiScaleFeaturePoints<T>: PointsProvider<T>
    {
        IImageD_Provider imaged_provider;
        List<IPointsProvider<T>> points_providers;
        double scale_factor;
        public MultiScaleFeaturePoints(IImageD_Provider imaged_provider, Func<IImageD_Provider, IPointsProvider<T>> points_provider_gen, double level, double scale_factor, double gaussian_ro)
        {
            this.imaged_provider = imaged_provider;
            this.scale_factor = scale_factor;
            this.points_providers = new List<IPointsProvider<T>>();
            for(int i=0;i<level;i++)
            {
                points_providers.Add(points_provider_gen(imaged_provider));
                imaged_provider = new ImageD_Providers.Scale(new ImageD_Providers.GaussianBlur(imaged_provider, gaussian_ro), scale_factor);
            }
        }
        protected override List<ImagePoint> GetPointsInternal()
        {
            List<ImagePoint<T>> ans = new List<ImagePoint<T>>();
            double scale = 1;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < points_providers.Count; i++, scale *= scale_factor)
            {
                var points = points_providers[i].GetPoints().Cast<ImagePoint<T>>()
                    .Select(p => new ImagePoint<T>(p.x / scale, p.y / scale, p.importance, p.content));
                sb.AppendLine($"{points.Count()} points from scale {scale}");
                ans.AddRange(points);
            }
            LogPanel.Log(sb.ToString());
            return ans.Cast<ImagePoint>().ToList();
        }
        public override void Reset()
        {
            base.Reset();
            imaged_provider.Reset();
            foreach (var points_provider in points_providers) points_provider.Reset();
        }
    }
}
