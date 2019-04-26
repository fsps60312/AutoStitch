using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AutoStitch.Pages
{
    class PlanePage:ContentControl
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
            public void move_to_center()
            {
                (double x, double y) = (images.Sum(i => i.center_x) / images.Count, images.Sum(i => i.center_y) / images.Count);
                foreach (var img in images) img.set_position(img.center_x - x, img.center_y - y);
            }
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
            public void set_position(double center_x,double center_y) { this.center_x = center_x;this.center_y = center_y; }
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
        class comparer1 : Comparer<Tuple<int,int, Tuple<double, double, int>>>
        {
            public override int Compare(Tuple<int, int, Tuple<double, double, int>> x, Tuple<int, int, Tuple<double, double, int>> y)
            {
                return -x.Item3.Item3.CompareTo(y.Item3.Item3);
            }
        }
        static int kase = 0;
        async void StartSimulation()
        {
            int kase_self = System.Threading.Interlocked.Increment(ref kase);
            await Task.Run(() =>
            {
                var images = source_image_panel.GetImages();
                List<IPointsProvider> points_providers = images.Select(i => new ImageD_Providers.ImageD_Cache(i.ToImageD()) as IImageD_Provider).Select(i => new PointsProviders.MSOP_DescriptorVector(new PointsProviders.MultiScaleHarrisCornerDetector(i), new MatrixProviders.GrayScale(i)) as IPointsProvider).ToList();
                List<IImageD_Provider> image_providers = images.Select((i, idx) => new ImageD_Providers.PlotPoints(new ImageD_Providers.ImageD_Cache(i.ToImageD()), points_providers[idx]) as IImageD_Provider).ToList();
                var global_viewer = new Func<PlaneImages>(() =>
                 {
                     return new PlaneImages(image_providers, 5000, 600);
                 })();
                int n = global_viewer.images.Count;
                image_container.Dispatcher.Invoke(() => image_container.Content = new ImageViewer(global_viewer, false));
                LogPanel.Log("searching features...");
                Parallel.For(0, n, i => image_providers[i].GetImageD());
                global_viewer.GetImageD();
                LogPanel.Log("matching...");
                var points = points_providers.Select(pp => pp.GetPoints().Select(ps => (ps as ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>)).ToList()).ToList();
                var get_displacement = new Func<int, int, Tuple<double, double,int>>((i, j) =>
                     {
                         List<ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>> p1s = points[i], p2s = points[j];
                         List<Tuple<double, double>> candidates = new List<Tuple<double, double>>();
                         Parallel.For(0, p1s.Count, _ =>
                           {
                               var p1 = p1s[_];
                               if (p1.content.try_match(p2s, out ImagePoint p2))
                               {
                                   IImageD_Provider me = image_providers[i], other = image_providers[j];
                                   double dx = (p2.x - 0.5 * other.GetImageD().width) - (p1.x - 0.5 * me.GetImageD().width);
                                   double dy = (p2.y - 0.5 * other.GetImageD().height) - (p1.y - 0.5 * me.GetImageD().height);
                                   lock (candidates) candidates.Add(new Tuple<double, double>(dx, dy));
                                 //candidates.Add(Tuple.Create(p1.content.difference((p2 as ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>).content), p1.x, p1.y, p2.x, p2.y));
                             }
                           });
                         var ans= Utils.Vote(candidates, 10, out int max_num_inliners);
                         return new Tuple<double, double, int>(ans.Item1, ans.Item2, max_num_inliners);
                     });
                {
                    bool[] vis = new bool[n];
                    SortedSet<Tuple<int,int, Tuple<double, double, int>>> edge_list = new SortedSet<Tuple<int,int, Tuple<double, double, int>>>(new comparer1());
                    var add_edges = new Action<int>(u =>
                      {
                          Parallel.For(0, n, i =>
                            {
                                if (!vis[i])
                                {
                                    var edge = new Tuple<int, int, Tuple<double, double, int>>(u, i, get_displacement(u, i));
                                    lock (edge_list) edge_list.Add(edge);
                                }
                            });
                      });
                    vis[0] = true; global_viewer.images[0].set_position(0, 0);
                    add_edges(0);
                    while(edge_list.Count>0)
                    {
                        var edge = edge_list.First();edge_list.Remove(edge);
                        if (vis[edge.Item2]) continue;
                        LogPanel.Log($"edge: {edge.Item1} → {edge.Item2}");
                        vis[edge.Item2] = true;
                        var u = global_viewer.images[edge.Item1];
                        global_viewer.images[edge.Item2].set_position(u.center_x - edge.Item3.Item1, u.center_y - edge.Item3.Item2);
                        add_edges(edge.Item2);
                    }
                }
                global_viewer.move_to_center();
                global_viewer.ResetSelf();
                global_viewer.GetImageD();
                LogPanel.Log("ok");
            });
        }
        public PlanePage()
        {
            InitializeViews();
        }
    }
}
