using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace AutoStitch.Pages
{
    class CylinderPage:ContentControl
    {
        class CylinderImages : ImageD_Provider
        {
            List<IImageD_Provider> providers;
            public List<CylinderImage> images { get; private set; }
            int width, height;
            public CylinderImages(List<IImageD_Provider> providers, int width, int height)
            {
                this.providers = providers;
                this.images = providers.Select(p => new CylinderImage(p, Utils.RandDouble() * 2.0 * Math.PI, 100)).ToList();
                this.width = width;
                this.height = height;
            }
            protected override MyImageD GetImageDInternal()
            {
                MyImageD image = new MyMatrix(new double[height, width]).ToGrayImageD();
                double min_h = images.Min(i => (i.displace_y - 0.5 * i.height) / i.focal_length),
                    max_h = images.Max(i => (i.displace_y + 0.5 * i.height) / i.focal_length);
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        double r = 0, g = 0, b = 0;
                        int cnt = 0;
                        foreach (var img in images)
                        {
                            if (img.sample_pixel(((double)j / width) * 2.0 * Math.PI, 1, (i * max_h + (height - 1 - i) * min_h) / (height - 1), out double _r, out double _g, out double _b))
                            {
                                r += _r; g += _g; b += _b;
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
                RowDefinitions =
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
        class comparer1 : Comparer<Tuple<int, int, Tuple<double, double, int>>>
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
                var global_viewer = new Func<CylinderImages>(() =>
                {
                    return new CylinderImages(image_providers, 5000, 600);
                })();
                int n = global_viewer.images.Count;
                image_container.Dispatcher.Invoke(() => image_container.Content = new ImageViewer(global_viewer, false));
                LogPanel.Log("searching features...");
                Parallel.For(0, n, i => image_providers[i].GetImageD());
                global_viewer.GetImageD();
                LogPanel.Log("matching...");
                var points = points_providers.Select(pp => pp.GetPoints().Select(ps => (ps as ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>)).ToList()).ToList();
                var get_displacement = new Func<int, int, Tuple<double, double, int>>((i, j) =>
                {
                    List<ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>> p1s = points[i], p2s = points[j];
                    List<Tuple<double, double>> candidates = new List<Tuple<double, double>>();
                    Parallel.For(0, p1s.Count, _ =>
                    {
                        var p1 = p1s[_];
                        if (p1.content.try_match(p2s, out ImagePoint p2))
                        {
                            IImageD_Provider me = image_providers[i], other = image_providers[j];
                            double dx = (p1.x - 0.5 * me.GetImageD().width) - (p2.x - 0.5 * other.GetImageD().width);
                            double dy = (p1.y - 0.5 * me.GetImageD().height) - (p2.y - 0.5 * other.GetImageD().height);
                            lock (candidates) candidates.Add(new Tuple<double, double>(dx, dy));
                            //candidates.Add(Tuple.Create(p1.content.difference((p2 as ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>).content), p1.x, p1.y, p2.x, p2.y));
                        }
                    });
                    var ans = Utils.Vote(candidates, 10, out int max_num_inliners);
                    return new Tuple<double, double, int>(ans.Item1, ans.Item2, max_num_inliners);
                });
                //{
                //    int[,] mat = new int[n, n];
                //    LogPanel.Log("matrix of num_acceptants:");
                //    StringBuilder sb = new StringBuilder();
                //    for (int i = 0; i < n; i++)
                //    {
                //        for (int j = 0; j < n; j++)
                //        {
                //            if (j > 0) sb.Append("\t");
                //            sb.Append($"{(mat[i, j] = get_displacement(i, j).Item3)}");
                //        }
                //        sb.AppendLine();
                //    }
                //    LogPanel.Log(sb.ToString());
                //}
                {
                    List<List<Tuple<int, Tuple<double, double, int>>>> edges = new List<List<Tuple<int, Tuple<double, double, int>>>>();
                    for (int i = 0; i < n; i++) edges.Add(new List<Tuple<int, Tuple<double, double, int>>>());
                    Parallel.For(0, n, i =>
                      {
                          for (int j = i + 1; j < n; j++)
                          {
                              var dis1 = get_displacement(i, j);
                              var dis2 = get_displacement(j, i);
                              double dx = (dis1.Item1 - dis2.Item1) / 2;
                              double dy = (dis1.Item2 - dis2.Item2) / 2;
                              int acceptants = Math.Min(dis1.Item3, dis2.Item3);
                              if (acceptants > 10)
                              {
                                  lock (edges[i]) edges[i].Add(new Tuple<int, Tuple<double, double, int>>(j, new Tuple<double, double, int>(dx, dy, acceptants)));
                                  lock (edges[j]) edges[j].Add(new Tuple<int, Tuple<double, double, int>>(i, new Tuple<double, double, int>(-dx, -dy, acceptants)));
                              }
                              //edge_list.Add(new Tuple<int, int, Tuple<double, double, int>>(i, j, new Tuple<double, double, int>(dx, dy, acceptants)));
                          }
                          LogPanel.Log($"finished matching from {i}");
                      });
                    LogPanel.Log("searching cycle...");
                    List<int> cycle = new List<int>();
                    List<double> dx_seq = new List<double>();
                    List<double> dy_seq = new List<double>();
                    {
                        bool[] vis = new bool[n];
                        int cur = 0;
                        while(true)
                        {
                            cycle.Add(cur);
                            vis[cur] = true;
                            int nxt_cur = -1;
                            double dx = double.MaxValue, dy = 0;
                            foreach(var e in edges[cur])
                            {
                                if(0<e.Item2.Item1&&e.Item2.Item1<dx)
                                {
                                    dx = e.Item2.Item1;
                                    dy = e.Item2.Item2;
                                    nxt_cur = e.Item1;
                                }
                            }
                            dx_seq.Add(dx);
                            dy_seq.Add(dy);
                            cur = nxt_cur;
                            if (cur == -1 || vis[cur])
                            {
                                LogPanel.Log($"cur: {cur}, visited: " + string.Join(", ", cycle));
                                if (cur != cycle[0]) throw new Exception("can't find a loop");
                                break;
                            }
                        }
                    }
                    double sum_dx = dx_seq.Sum(), sum_dy = dy_seq.Sum();
                    double prefix_sum_dx = 0, prefix_sum_dy = 0;
                    for(int i=0;i<cycle.Count;i++)
                    {
                        int u = cycle[i];
                        var img = global_viewer.images[u];
                        double width_ratio = img.width / sum_dx;
                        // atan(0.5*width/f)=width_ratio*PI
                        img.center_direction = prefix_sum_dx / sum_dx * 2.0 * Math.PI;
                        img.focal_length = 0.5 * img.width / Math.Tan(width_ratio * Math.PI);
                        img.rotation = 0;
                        img.scalar = 1;
                        img.displace_x = 0;
                        img.displace_y = prefix_sum_dy - sum_dy * (prefix_sum_dx / sum_dx);
                        prefix_sum_dx += dx_seq[i];
                        prefix_sum_dy += dy_seq[i];
                    }
                }
                //global_viewer.move_to_center();
                global_viewer.ResetSelf();
                global_viewer.GetImageD();
                LogPanel.Log("ok");
            });
        }
        class CylinderImage
        {
            //  [   αcosθ   -αsinθ  x'  ][x]   [xαcosθ-yαsinθ+x']
            //  [   αsinθ   αcosθ   y'  ][y] = [xαsinθ+yαcosθ+y']
            //  [   0       0       1   ][1]   [       1        ]
            //  h=(xαsinθ+yαcosθ+y')/f
            //  w=t+atan((xαcosθ-yαsinθ+x')/f)
            //  params: α, θ, x', y', f, t
            //  have: pairwise (h,w), (H,W)
            //  minimize: β(h-H)^2+(w-W)^2
            //
            //  d: β(h-H)(dh-dH)+(w-W)(dw-dW)=0
            //  dh/dα:  (xsinθ+ycosθ)/f
            //  dw/dα:  ((xcosθ-ysinθ)/f)/(1+((xαcosθ-yαsinθ+x')/f)^2)
            //  dh/dθ:  (xαcosθ-yαsinθ)/f
            //  dw/dθ:  ((-xαsinθ-yαcosθ)/f)/(1+((xαcosθ-yαsinθ+x')/f)^2)
            //  dh/dx': 0
            //  dw/dx': (1/f)/(1+((xαcosθ-yαsinθ+x')/f)^2)
            //  dh/dy': 1/f
            //  dw/dy': 0
            //  dh/df:  -(xαsinθ+yαcosθ+y')/f^2
            //  dw/df:  (-(xαcosθ-yαsinθ+x')/f^2)/(1+((xαcosθ-yαsinθ+x')/f)^2)
            //  dh/dt:  0
            //  dw/dt:  1
            IImageD_Provider image_provider;
            public double center_direction;
            public double focal_length;
            public double rotation = 0, scalar = 1, displace_x = 0, displace_y = 0;
            public (double, double) image_point_to_camera(double x, double y)
            {
                x -= 0.5 * width;y -= 0.5 * height;
                return (
                    center_direction + Math.Atan((x * scalar * Math.Cos(rotation) - y * scalar * Math.Sin(rotation) + displace_x) / focal_length),
                    (x * scalar * Math.Sin(rotation) + y * scalar * Math.Cos(rotation) + displace_y) / focal_length);

            }
            public double height { get { return image_provider.GetImageD().height; } }
            public double width { get { return image_provider.GetImageD().width; } }
            public CylinderImage(IImageD_Provider image_provider,double center_direction,double focal_length)
            {
                this.image_provider = image_provider;
                this.center_direction = center_direction;
                this.focal_length = focal_length;
            }
            public bool sample_pixel(double direction, double cylinder_radius, double h,out double r,out double g,out double b)
            {
                // h = y*(r/sqrt(x^2+f^2))
                // a = center_direction+atan(x/f)
                // r = 1, f fixed, for each "h, a", find "x, y"
                double angle_diff = (direction - center_direction) % (2.0 * Math.PI);
                if (angle_diff < 0) angle_diff += 2.0 * Math.PI;
                if (0.5 * Math.PI <= angle_diff && angle_diff <= 1.5 * Math.PI) { r = g = b = 0; return false; }
                MyImageD image = image_provider.GetImageD();
                double x = Math.Tan(direction - center_direction) * focal_length;
                double y = h * (Math.Sqrt(x * x + focal_length * focal_length) / cylinder_radius);
                x += 0.5 * width;y += 0.5 * height;
                (x, y) = ((x * Math.Cos(-rotation) - y * Math.Sin(-rotation)) / scalar, (x * Math.Sin(-rotation) + y * Math.Cos(-rotation)) / scalar);
                return image.sample(x, y, out r, out g, out b);
            }
        }
        public CylinderPage()
        {
            InitializeViews();
        }
    }
}
