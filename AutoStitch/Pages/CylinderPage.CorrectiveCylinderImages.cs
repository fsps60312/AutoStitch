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
            const int min_num_inliners = 10;
            public CorrectiveCylinderImages(List<IImageD_Provider> image_providers, int width, int height):base(image_providers,width,height)
            {
                points_providers= this.image_providers.Select(i => new PointsProviders.MSOP_DescriptorVector(new PointsProviders.MultiScaleHarrisCornerDetector(i), new MatrixProviders.GrayScale(i)) as IPointsProvider).ToList();
            }
            List<List<ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>>> points;
            List<int>[,] points_match_list_cache = null;
            private List<int>points_match_list(int i,int j)
            {
                System.Diagnostics.Trace.Assert(0 <= i && i < n && 0 <= j && j < n);
                if (points_match_list_cache == null) points_match_list_cache = new List<int>[n, n];
                if (points_match_list_cache[i, j] == null) points_match_list_cache[i, j] = PointsProviders.MSOP_DescriptorVector.Descriptor.try_match(points[i], points[j]);
                return points_match_list_cache[i, j];
            }
            private Tuple<double, double, int> get_displacement(int i, int j)
            {
                List<int> matches = points_match_list(i, j);
                List<Tuple<double, double>> candidates = new List<Tuple<double, double>>();
                for (int _ = 0; _ < matches.Count; _++)
                {
                    if (matches[_] != -1)
                    {
                        var p1 = points[i][_];
                        var p2 = points[j][matches[_]];
                        IImageD_Provider me = image_providers[i], other = image_providers[j];
                        double dx = (p1.x - 0.5 * me.GetImageD().width) - (p2.x - 0.5 * other.GetImageD().width);
                        double dy = (p1.y - 0.5 * me.GetImageD().height) - (p2.y - 0.5 * other.GetImageD().height);
                        candidates.Add(new Tuple<double, double>(dx, dy));
                    }
                }
                //LogPanel.Log($"candidate.count={candidates.Count}");
                var ans = Utils.Vote(candidates, 10, out int max_num_inliners);
                return new Tuple<double, double, int>(ans.Item1, ans.Item2, max_num_inliners);
            }
            List<int> cycle;
            List<double> dx_seq;
            List<double> dy_seq;
            private void search_cycle()
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for(int i=0;i<n;i++)
                {
                    for(int j=0;j<n;j++)
                    {
                        if (j > 0) sb.Append('\t');
                        sb.Append(get_displacement(i, j).Item3);
                    }
                    sb.AppendLine();
                }
                LogPanel.Log("matrix of acceptants:");
                LogPanel.Log(sb.ToString());
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
                        if (acceptants >= min_num_inliners)
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
            int refine_count = 0;
            double step_size = double.NaN;
            public void Refine(bool verbose=true)
            {
                ++refine_count;
                if (verbose) LogPanel.Log($"refining #{refine_count}...");
                double total_error = 0;
                double d_sum = 0;
                List<(double, double, double, double, double, double,double,double)> derivatives = new List<(double, double, double, double, double, double,double,double)>();
                int num_error_entries = 0;
                System.Text.StringBuilder sb_inliners = new System.Text.StringBuilder();
                double average_focal_length = cylinder_images.Sum(i => i.focal_length) / cylinder_images.Count;
                for(int i=0;i<n;i++)
                {
                    List<Tuple<Tuple<double, double>, Tuple<double, double>, CylinderImage>> matches = new List<Tuple<Tuple<double, double>, Tuple<double, double>, CylinderImage>>();
                    for (int j=0;j<n;j++)
                    {
                        if(j>0)sb_inliners.Append('\t');
                        if (i == j) continue;
                        List<int> match_list_raw = points_match_list(i, j);
                        List<(int, int)> match_list = new List<(int, int)>();
                        for (int k = 0; k < match_list_raw.Count; k++) if (match_list_raw[k] != -1) match_list.Add((k, match_list_raw[k]));
                        var inliners = Utils.VoteInliners(match_list.Select(_ =>
                          {
                              (int p, int q) = _;
                              (double x1, double y1) = cylinder_images[i].image_point_to_camera(points[i][p].x, points[i][p].y);
                              (double x2, double y2) = cylinder_images[j].image_point_to_camera(points[j][q].x, points[j][q].y);
                              if (x2 - x1 > Math.PI) x1 += 2.0 * Math.PI;
                              else if (x1 - x2 > Math.PI) x2 += 2.0 * Math.PI;
                              //LogPanel.Log($"{x1},{y1}\t{x2},{y2}");
                              return Tuple.Create(x1 - x2, (y1 - y2) * Math.PI);
                          }).ToList(), 10 / average_focal_length);
                        sb_inliners.Append(inliners.Count);
                        if (inliners.Count >= min_num_inliners)
                        {
                            foreach (var k in inliners)
                            {
                                (int src, int tar) = match_list[k];
                                //if (Utils.RandDouble() < 0.1)
                                //{
                                //    (double x1, double y1) = (points[i][src].x, points[i][src].y);
                                //    (double x2, double y2) = (points[j][tar].x, points[j][tar].y);
                                //    (x1, y1) = cylinder_images[i].image_point_to_camera(x1, y1);
                                //    (x2, y2) = cylinder_images[j].image_point_to_camera(x2, y2);
                                //    LogPanel.Log($"{x1}\t{x2}\t{y1}\t{y2}");
                                //}
                                matches.Add(new Tuple<Tuple<double, double>, Tuple<double, double>, CylinderImage>(
                                    new Tuple<double, double>(points[i][src].x, points[i][src].y),
                                    new Tuple<double, double>(points[j][tar].x, points[j][tar].y),
                                    cylinder_images[j]));
                            }
                        }
                    }
                    sb_inliners.AppendLine();
                    num_error_entries += matches.Count;
                    cylinder_images[i].get_derivatives(Math.PI, matches, out double alpha, out double theta, out double dx, out double dy, out double df, out double dt,out double dp,out double dq, out double error);
                    total_error += error;
                    d_sum += alpha * alpha + theta * theta + dx * dx + dy * dy + df * df + dt * dt + dp * dp + dq * dq;
                    derivatives.Add((alpha, theta, dx, dy, df, dt,dp,dq));
                }
                if (verbose)
                {
                    LogPanel.Log("number of inliners matrix:");
                    LogPanel.Log(sb_inliners.ToString());
                    LogPanel.Log($"average error: {total_error / num_error_entries}");
                }
                if (double.IsNaN(step_size)) step_size = total_error / d_sum * 0.5;
                else step_size = total_error / d_sum * 0.5;
                if (verbose) LogPanel.Log($"step_size: {step_size}");
                for (int i=0;i<n;i++)
                {
                    (double alpha, double theta, double dx, double dy, double df, double dt,double dp,double dq) = derivatives[i];
                    if (verbose) LogPanel.Log($"image[{i}], alpha = {alpha}, theta = {theta}, dx = {dx}, dy = {dy}, df = {df}, dt = {dt}, dp = {dp}, dq = {dq}");
                    cylinder_images[i].scalar_alpha -= step_size * alpha;
                    cylinder_images[i].rotation_theta -= step_size * theta;
                    cylinder_images[i].displace_x -= step_size * dx;
                    cylinder_images[i].displace_y -= step_size * dy;
                    cylinder_images[i].focal_length -= step_size * df;
                    cylinder_images[i].center_direction -= step_size * dt;
                    cylinder_images[i].perspective_x -= step_size * dp;
                    cylinder_images[i].perspective_y -= step_size * dq;
                }
                if (verbose)
                {
                    LogPanel.Log("done.");
                    this.ResetSelf();
                    this.GetImageD();
                }
                //System.Threading.Thread.Sleep(1000000000);
            }
            public void InitializeOnPlane()
            {
                this.ResetSelf();
                this.GetImageD();
                LogPanel.Log("searching features...");
                Parallel.For(0, n, i => { int c = points_providers[i].GetPoints().Count; LogPanel.Log($"{c} features for image {i}"); });
                points = points_providers.Select(pp => pp.GetPoints().Select(ps => (ps as ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>)).ToList()).ToList();

                setup_image_params();
                this.ResetSelf();
                this.GetImageD();
                LogPanel.Log("ok");
            }
        }
    }
}
