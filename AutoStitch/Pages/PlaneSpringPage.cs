using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AutoStitch.Pages
{
    class PlaneSpringPage:ContentControl
    {
        class PlaneImages : ImageD_Provider
        {
            List<IImageD_Provider> providers;
            public List<PlaneImage> images { get; private set; }
            int width, height;
            public PlaneImages(List<IImageD_Provider>providers,int width,int height)
            {
                this.providers = providers;
                this.images = providers.Select(p => new PlaneImage(p, 0.5 * width + (Utils.RandDouble() - 0.5) * 500, 0.5 * height + (Utils.RandDouble() - 0.5) * 300)).ToList();
                this.width = width;
                this.height = height;
            }
            public void update_speed(double dt) { foreach (var i in images) i.update_speed(dt); }
            public void update_position() { foreach (var i in images) i.update_position(); }
            protected override MyImageD GetImageDInternal()
            {
                MyImageD image = new MyMatrix(new double[height, width]).ToGrayImageD();
                double center_x = images.Sum(i => i.center_x) / images.Count, center_y = images.Sum(i => i.center_y) / images.Count;
                for(int i = 0; i < height; i++)
                {
                    for(int j=0;j<width;j++)
                    {
                        double r = 0, g = 0, b = 0;
                        double x = j + (center_x - 0.5 * width), y = i + (center_y - 0.5 * height);
                        int cnt = 0;
                        foreach (var img in images)
                        {
                            if (img.sample_pixel(x, y, out double _r, out double _g, out double _b))
                            {
                                r += _r;g += _g;b += _b;
                                cnt++;
                            }
                        }
                        if (cnt > 0) { r /= cnt; g /= cnt; b /= cnt; }
                        int k = i * image.stride + j * 4;
                        // bgra
                        image.data[k + 0] = b;
                        image.data[k + 1] = g;
                        image.data[k + 2] = r;
                        image.data[k + 3] = 1;
                    }
                }
                return image;
            }
            public override void Reset()
            {
                base.Reset();
                foreach (var provider in providers) provider.Reset();
            }
        }
        class PlaneImage
        {
            IImageD_Provider image_provider;
            public double center_x { get; private set; }
            public double center_y { get; private set; }
            double speed_x = 0, speed_y = 0;
            const double damping = 0.5;
            class Spring
            {
                PlaneImage me;
                PlaneImage other;
                double dx, dy;
                const double spring_coefficient = 0.001;
                public void get_force(out double force_x, out double force_y)
                {
                    force_x = spring_coefficient * Math.Min(10, (other.center_x - dx - me.center_x));
                    force_y = spring_coefficient * Math.Min(10, (other.center_y - dy - me.center_y));
                }
                public Spring(PlaneImage me,PlaneImage other,double dx,double dy)
                {
                    this.me = me;
                    this.other = other;
                    this.dx = dx;
                    this.dy = dy;
                }
            }
            public static void AddSpring(PlaneImage me,PlaneImage other,double from_x,double from_y,double to_x,double to_y)
            {
                double dx = (to_x - 0.5 * other.image_provider.GetImageD().width) - (from_x - 0.5 * me.image_provider.GetImageD().width);
                double dy = (to_y - 0.5 * other.image_provider.GetImageD().height) - (from_y - 0.5 * me.image_provider.GetImageD().height);
                LogPanel.Log($"dx={dx}, dy={dy}");
                me.springs.Add(new Spring(me, other, -dx, -dy));
                other.springs.Add(new Spring(other, me, dx, dy));
            }
            double cache_dt = 0;
            public void update_position()
            {
                center_x += speed_x * cache_dt;
                center_y += speed_y * cache_dt;
                speed_x *= Math.Pow(damping, cache_dt);
                speed_y *= Math.Pow(damping, cache_dt);
                cache_dt = 0;
            }
            public void update_speed(double dt)
            {
                foreach(var s in springs)
                {
                    s.get_force(out double force_x, out double force_y);
                    speed_x += force_x;
                    speed_y += force_y;
                }
                cache_dt += dt;
            }
            List<Spring> springs = new List<Spring>();
            public PlaneImage(IImageD_Provider image_provider, double center_x, double center_y)
            {
                this.image_provider = image_provider;
                this.center_x = center_x;
                this.center_y = center_y;
            }
            public bool sample_pixel(double x, double y, out double r, out double g, out double b)
            {
                // h = y*(r/sqrt(x^2+f^2))
                // a = center_direction+atan(x/f)
                // r = 1, f fixed, for each "h, a", find "x, y"
                MyImageD image = image_provider.GetImageD();
                return image.sample(x - (this.center_x - 0.5 * image.width), y - (this.center_y - 0.5 * image.height), out r, out g, out b);
            }
        }
        SourceImagePanel source_image_panel = new SourceImagePanel(false);
        ContentControl image_container = new ContentControl();
        private void InitializeViews()
        {
            this.Content = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)},
                    new ColumnDefinition{Width=new GridLength(3,GridUnitType.Star)}
                },
                RowDefinitions=
                {
                    new RowDefinition{Height=new GridLength(1,GridUnitType.Auto)},
                    new RowDefinition{Height=new GridLength(1,GridUnitType.Star)}
                },
                Children =
                {
                    new Button{Content="Run"}.Set(()=>
                    {
                        StartSimulation();
                    }).Set(0,0),
                    source_image_panel.Set(1,0),
                    image_container.Set(0,1).SetSpan(2,1)
                }
            };
        }
        static int kase = 0;
        async void StartSimulation()
        {
            int kase_self = System.Threading.Interlocked.Increment(ref kase);
            await Task.Run(() =>
            {
                List<IPointsProvider> points_providers = source_image_panel.GetImages().Select(i => new ImageD_Providers.ImageD_Cache(i.ToImageD()) as IImageD_Provider).Select(i => new PointsProviders.MSOP_DescriptorVector(new PointsProviders.MultiScaleHarrisCornerDetector(i), new MatrixProviders.GrayScale(i)) as IPointsProvider).ToList();
                List<IImageD_Provider> image_providers = source_image_panel.GetImages().Select((i, idx) => new ImageD_Providers.PlotPoints(new ImageD_Providers.ImageD_Cache(i.ToImageD()), points_providers[idx]) as IImageD_Provider).ToList();
                var provider = new PlaneImages(image_providers, 1000, 600);
                image_container.Dispatcher.Invoke(() => image_container.Content = new ImageViewer(provider,false));
                LogPanel.Log("searching features...");
                provider.GetImageD();
                LogPanel.Log("matching...");
                var points = points_providers.Select(pp => pp.GetPoints().Select(ps => (ps as ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>)).ToList()).ToList();
                for(int i=0;i<points.Count;i++)
                {
                    for(int j=i+1;j<points.Count;j++)
                    {
                        List<ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>> p1s = points[i], p2s = points[j];
                        List<Tuple<double, double, double, double, double>> candidates = new List<Tuple<double, double, double, double, double>>();
                        foreach(var p1 in p1s)
                        {
                            if(p1.content.try_match(p2s, out ImagePoint p2))
                            {
                                candidates.Add(Tuple.Create(p1.content.difference((p2 as ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>).content), p1.x, p1.y, p2.x, p2.y));
                            }
                        }
                        candidates.Sort((p1, p2) => p1.Item1.CompareTo(p2.Item1));
                        for (int k = 0; k < candidates.Count; k++)
                        {
                            var c = candidates[k];
                            PlaneImage.AddSpring(provider.images[i], provider.images[j], c.Item2, c.Item3, c.Item4, c.Item5);
                        }
                    }
                }
                LogPanel.Log("ok");
                while (kase_self == kase)
                {
                    provider.update_speed(0.1);
                    provider.update_position();
                    provider.ResetSelf();
                    provider.GetImageD();
                }
            });
        }
        public PlaneSpringPage()
        {
            InitializeViews();
        }
    }
}
