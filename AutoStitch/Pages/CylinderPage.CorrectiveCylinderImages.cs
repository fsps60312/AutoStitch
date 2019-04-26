using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoStitch.Pages
{
    partial class CylinderPage
    {
        class CorrectiveCylinderImages :CylinderImages
        {
            List<IPointsProvider> points_providers;
            private int n { get { return cylinder_images.Count; } }
            public CorrectiveCylinderImages(List<IImageD_Provider> image_providers, int width, int height):base(image_providers,width,height)
            {
                points_providers= this.image_providers.Select(i => new PointsProviders.MSOP_DescriptorVector(new PointsProviders.MultiScaleHarrisCornerDetector(i), new MatrixProviders.GrayScale(i)) as IPointsProvider).ToList();
            }
            List<List<ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>>> points;
            private Tuple<double, double, int> get_displacement(int i,int j)
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
            }
            List<int> cycle;
            List<double> dx_seq;
            List<double> dy_seq;
            private void search_cycle()
            {
                LogPanel.Log("building matching graph...");
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
                cycle = new List<int>();
                dx_seq = new List<double>();
                dy_seq = new List<double>();
                {
                    bool[] vis = new bool[n];
                    int cur = 0;
                    while (true)
                    {
                        cycle.Add(cur);
                        vis[cur] = true;
                        int nxt_cur = -1;
                        double dx = double.MaxValue, dy = 0;
                        foreach (var e in edges[cur])
                        {
                            if (0 < e.Item2.Item1 && e.Item2.Item1 < dx)
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
            }
            private void setup_image_params()
            {
                search_cycle();
                double sum_dx = dx_seq.Sum(), sum_dy = dy_seq.Sum();
                double prefix_sum_dx = 0, prefix_sum_dy = 0;
                for (int i = 0; i < cycle.Count; i++)
                {
                    int u = cycle[i];
                    var img = cylinder_images[u];
                    double width_ratio = img.width / sum_dx;
                    // atan(0.5*width/f)=width_ratio*PI
                    img.center_direction = prefix_sum_dx / sum_dx * 2.0 * Math.PI;
                    img.focal_length = 0.5 * img.width / Math.Tan(width_ratio * Math.PI);
                    img.rotation_theta = 0;
                    img.scalar_alpha = 1;
                    img.displace_x = 0;
                    img.displace_y = prefix_sum_dy - sum_dy * (prefix_sum_dx / sum_dx);
                    prefix_sum_dx += dx_seq[i];
                    prefix_sum_dy += dy_seq[i];
                }
            }
            public void InitializeOnPlane()
            {
                this.ResetSelf();
                this.GetImageD();
                LogPanel.Log("searching features...");
                Parallel.For(0, n, i => points_providers[i].GetPoints());
                points = points_providers.Select(pp => pp.GetPoints().Select(ps => (ps as ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>)).ToList()).ToList();
                this.ResetSelf();
                this.GetImageD();
                setup_image_params();
                this.ResetSelf();
                this.GetImageD();
                LogPanel.Log("ok");
            }
        }
    }
}
