using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.PointsProviders
{
    class HarrisCornerDetector:PointsProvider
    {
        IPointsProvider points_provider;
        public HarrisCornerDetector(IImageD_Provider image_provider)
        {
            IMatrixProvider mp_hr = new MatrixProviders.HarrisDetectorResponse(MatrixProviders.Filter.Red(image_provider));
            IMatrixProvider mp_hg = new MatrixProviders.HarrisDetectorResponse(MatrixProviders.Filter.Green(image_provider));
            IMatrixProvider mp_hb = new MatrixProviders.HarrisDetectorResponse(MatrixProviders.Filter.Blue(image_provider));
            IMatrixProvider mp_harris = new MatrixProviders.Add(mp_hr, mp_hg, mp_hb);
            IPointsProvider
                pp_harris_filtered = new PointsProviders.LocalMaximum(mp_harris, 10 * 3.0 / (255.0 * 255.0)),
                pp_harris_refined = new PointsProviders.SubpixelRefinement(pp_harris_filtered, mp_harris),
                pp_harris_eliminated = new PointsProviders.AdaptiveNonmaximalSuppression(pp_harris_refined, 500);
            this.points_provider = pp_harris_eliminated;
        }
        public override void Reset()
        {
            base.Reset();
            points_provider.Reset();
        }
        protected override List<ImagePoint> GetPointsInternal()
        {
            return points_provider.GetPoints();
        }
    }
}
