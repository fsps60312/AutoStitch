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
            const int min_num_inliners = 25;
            static IPointsProvider get_features_provider(IImageD_Provider image_provider)
            {
                var points_provider_gen = new Func<IImageD_Provider, IPointsProvider<PointsProviders.MSOP_DescriptorVector.Descriptor>>(
                         i => new PointsProviders.MSOP_DescriptorVector(new PointsProviders.HarrisCornerDetector(i), new MatrixProviders.GrayScale(i)));
                return new PointsProviders.AdaptiveNonmaximalSuppression(
                    new PointsProviders.MultiScaleFeaturePoints<PointsProviders.MSOP_DescriptorVector.Descriptor>(
                     image_provider, points_provider_gen, 7, 0.5, 1), 1000) as IPointsProvider;
            }
            public CorrectiveCylinderImages(List<IImageD_Provider> image_providers, int width, int height):base(
                image_providers,
                //image_providers.Select(i=>new ImageD_Providers.PlotPoints(i, get_features_provider(i)) as IImageD_Provider).ToList(),
                width,height)
            {
                points_providers = image_providers.Select(img => get_features_provider(img)).ToList();
                {
                    var provider = image_providers[0];
                    double scale = 1;
                    for (int i = 0; i < 7; i++, scale *= 0.5)
                    {
                        MyImageD img = provider.GetImageD();
                        LogPanel.Log($"scale={scale}, width={img.width}, height={img.height}, stride={img.stride}, avg={img.data.Sum() / img.data.Length}");
                        LogPanel.Log(img);
                        provider = new ImageD_Providers.GaussianBlur(provider, 1);
                        provider = new ImageD_Providers.Scale(provider, 0.5);
                    }
                }
            }
            List<List<ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>>> points;
            List<int>[,] points_match_list_cache = null;
            object points_match_list_cache_sync = new object();
            private List<int>points_match_list(int i,int j)
            {
                System.Diagnostics.Trace.Assert(0 <= i && i < n && 0 <= j && j < n);
                lock (points_match_list_cache_sync)
                {
                    if (points_match_list_cache == null) points_match_list_cache = new List<int>[n, n];
                    if (points_match_list_cache[i, j] == null) points_match_list_cache[i, j] = PointsProviders.MSOP_DescriptorVector.Descriptor.try_match(points[i], points[j]);
                }
                return points_match_list_cache[i, j];
            }
            Tuple<double, double, int>[,] get_displacement_cache = null;
            object get_displacement_cache_sync = new object();
            private Tuple<double, double, int> get_displacement(int i, int j)
            {
                lock (get_displacement_cache_sync)
                {
                    if (get_displacement_cache == null) get_displacement_cache = new Tuple<double, double, int>[n, n];
                    if (get_displacement_cache[i, j] == null)
                    {
                        List<int> matches = points_match_list(i, j);
                        List<Tuple<double, double, double, double>> candidates = new List<Tuple<double, double, double, double>>();
                        for (int _ = 0; _ < matches.Count; _++)
                        {
                            if (matches[_] != -1)
                            {
                                var p1 = points[i][_];
                                var p2 = points[j][matches[_]];
                                IImageD_Provider me = image_providers[i], other = image_providers[j];
                                candidates.Add(new Tuple<double, double, double, double>(
                                    p1.x - 0.5 * me.GetImageD().width,
                                    p1.y - 0.5 * me.GetImageD().height,
                                    p2.x - 0.5 * other.GetImageD().width,
                                    p2.y - 0.5 * other.GetImageD().height));
                            }
                        }
                        //LogPanel.Log($"candidate.count={candidates.Count}");
                        var ans = Utils.VoteInliners(candidates, 10);
                        get_displacement_cache[i, j] = new Tuple<double, double, int>(
                            ans.Average(_ =>
                            {
                                var v = candidates[_];
                                return v.Item1 - v.Item3;
                            }),
                            ans.Average(_ =>
                            {
                                var v = candidates[_];
                                return v.Item2 - v.Item4;
                            }), ans.Count);
                    }
                }
                return get_displacement_cache[i, j];
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
                    img.transform.center_direction = prefix_sum_dx / sum_dx * 2.0 * Math.PI;
                    img.transform.focal_length = 0.5 * img.width / Math.Tan(width_ratio * Math.PI);
                    img.transform.rotation_theta = 0;
                    img.transform.scalar_x = 1;
                    img.transform.scalar_y = 1;
                    img.transform.displace_x = 0;
                    img.transform.displace_y = prefix_sum_dy - sum_dy * (prefix_sum_dx / sum_dx);
                    prefix_sum_dx += dx_seq[i];
                    prefix_sum_dy += dy_seq[i];
                }
            }
            //List<List<Tuple<Tuple<double, double>, Tuple<double, double>, CylinderImage>>> get_all_matches_cache = null;
            //object get_all_matches_sync = new object();
            private List<List<Tuple<Tuple<double, double>, Tuple<double, double>, CylinderImage>>> get_all_matches(bool verbose)
            {
                //lock (get_all_matches_sync)
                //{
                //    if (get_all_matches_cache == null)
                //    {
                System.Text.StringBuilder sb_inliners = new System.Text.StringBuilder();
                double average_focal_length = cylinder_images.Sum(i => i.focal_length) / cylinder_images.Count;
                List<List<Tuple<Tuple<double, double>, Tuple<double, double>, CylinderImage>>> all_matches = new List<List<Tuple<Tuple<double, double>, Tuple<double, double>, CylinderImage>>>();
                for (int i = 0; i < n; i++)
                {
                    List<Tuple<Tuple<double, double>, Tuple<double, double>, CylinderImage>> matches = new List<Tuple<Tuple<double, double>, Tuple<double, double>, CylinderImage>>();
                    for (int j = 0; j < n; j++)
                    {
                        if (j > 0) sb_inliners.Append('\t');
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
                            return Tuple.Create(x1 - x2, y1 - y2);
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
                    all_matches.Add(matches);
                }
                if (verbose)
                {
                    System.Diagnostics.Trace.WriteLine("number of inliners matrix:");
                    System.Diagnostics.Trace.WriteLine(sb_inliners.ToString());
                }
                //        get_all_matches_cache = all_matches;
                //    }
                //}
                //return get_all_matches_cache;
                return all_matches;
            }
            public int refine_count { get; private set; } = 0;
            public const int maximum_freedom = 10;
            public bool Test(int entry,bool verbose)
            {
                var all_matches = get_all_matches(verbose);
                int num_error_entries = all_matches.Sum(m => m.Count);
                double average_focal_length = cylinder_images.Sum(i => i.focal_length) / cylinder_images.Count;
                var average_pixel_error = new Func<double, double>(error => Math.Pow(error / num_error_entries, 0.5) * average_focal_length);
                List<(CylinderImage.Transform, double)> info = new List<(CylinderImage.Transform, double)>();
                for (int i = 0; i < n; i++)
                {
                    var matches = all_matches[i];
                    (CylinderImage.Transform derivative, double error) = cylinder_images[i].get_derivatives(matches);
                    if (entry != 1) derivative.center_direction = 0;
                    if (entry != 2) derivative.displace_y = 0;
                    if (entry != 3) derivative.rotation_theta = 0;
                    if (entry != 4) derivative.perspective_y = 0;
                    if (entry != 5) derivative.perspective_x = 0;
                    if (entry != 6) derivative.focal_length = 0;
                    if (entry != 7) derivative.scalar_y = 0;
                    if (entry != 8) derivative.scalar_x = 0;
                    if (entry != 9) derivative.displace_x = 0;
                    if (entry != 10) derivative.skew = 0;
                    info.Add((derivative, error));
                }
                double total_error = info.Sum(v => v.Item2);
                double d_sum = info.Sum(v => v.Item1.square_sum());
                if (verbose) LogPanel.Log($"average error: { average_pixel_error(total_error)}");
                //if (double.IsNaN(step_size)) step_size = total_error / d_sum * 0.1;
                //else step_size = total_error / d_sum * 0.1;
                for (int i = 0; i < n; i++) cylinder_images[i].save();
                var restore_all = new Action(() => { for (int i = 0; i < n; i++) cylinder_images[i].restore(); });
                var apply_change_all = new Func<double, double>(_ =>
                {
                    restore_all();
                    for (int i = 0; i < n; i++) cylinder_images[i].apply_change(info[i].Item1 * -(_ * total_error));
                    double ans = 0;
                    for (int i = 0; i < n; i++) ans += cylinder_images[i].get_derivatives( all_matches[i]).Item2;
                    return ans;
                });
                double current_error = apply_change_all(-1e-16);
                apply_change_all(0);
                return current_error >= total_error;
            }
            public bool Refine(int freedom, bool verbose)
            {
                ++refine_count;
                if (verbose) LogPanel.Log($"refining #{refine_count}...");
                var all_matches = get_all_matches(verbose);
                int num_error_entries = all_matches.Sum(m => m.Count);
                double average_focal_length = cylinder_images.Sum(i => i.focal_length) / cylinder_images.Count;
                var average_pixel_error = new Func<double, double>(error => Math.Pow(error / num_error_entries, 0.5) * average_focal_length);
                List<(CylinderImage.Transform, double)> info = new List<(CylinderImage.Transform, double)>();
                for (int i = 0; i < n; i++)
                {
                    var matches = all_matches[i];
                    (CylinderImage.Transform derivative, double error) = cylinder_images[i].get_derivatives( matches);
                    //if (freedom != 1) derivative.center_direction = 0;
                    //if (freedom != 2) derivative.displace_y = 0;
                    //if (freedom != 3) derivative.rotation_theta = 0;
                    //if (freedom != 4) derivative.perspective_y = 0;
                    //if (freedom != 5) derivative.focal_length = 0;
                    //if (freedom != 6) derivative.perspective_x = 0;
                    //if (freedom != 7) derivative.scalar_y = 0;
                    //if (freedom != 8) derivative.scalar_x = 0;
                    //if (freedom != 9) derivative.displace_x = 0;
                    //if (freedom != 10) derivative.skew = 0;
                    if (freedom != 1) derivative.center_direction = 0;
                    if (freedom != 2) derivative.displace_y = 0;
                    if (freedom != 3) derivative.rotation_theta = 0;
                    if (freedom != 4) derivative.perspective_y = 0;
                    if (freedom != 5) derivative.focal_length = 0;
                    if (freedom != 6) derivative.perspective_x = 0;
                    if (freedom != 7) derivative.scalar_y = 0;
                    if (freedom != 8) derivative.scalar_x = 0;
                    if (freedom != 9) derivative.displace_x = 0;
                    if (freedom != 10) derivative.skew = 0;
                    info.Add((derivative, error));
                }
                double total_error = info.Sum(v => v.Item2);
                double d_sum = info.Sum(v => v.Item1.square_sum());
                if (verbose) LogPanel.Log($"average error: { average_pixel_error(total_error)}");
                //if (double.IsNaN(step_size)) step_size = total_error / d_sum * 0.1;
                //else step_size = total_error / d_sum * 0.1;
                for (int i = 0; i < n; i++) cylinder_images[i].save();
                var restore_all = new Action(() => { for (int i = 0; i < n; i++) cylinder_images[i].restore(); });
                var apply_change_all = new Func<double, double>(_ =>
                 {
                     restore_all();
                     for (int i = 0; i < n; i++) cylinder_images[i].apply_change(info[i].Item1 * -(_ * total_error));
                     double ans = 0;
                     for (int i = 0; i < n; i++) ans += cylinder_images[i].get_derivatives( all_matches[i]).Item2;
                     return ans;
                 });
                double multiplier = 1e-15;
                double current_error = apply_change_all(multiplier);
                for (double nxt_error; (nxt_error = apply_change_all(multiplier * 2)) <= current_error; current_error = nxt_error, multiplier *= 2) ;
                if (current_error >= total_error)
                {
                    double pixel_error = average_pixel_error(apply_change_all(0));
                    if (verbose)
                    {
                        LogPanel.Log($"refine #{refine_count}: cannot improve, pixel error = {pixel_error}");
                        for (int i = 0; i < n; i++)
                        {
                            LogPanel.Log($"derivatives of image[{i}]:");
                            LogPanel.Log(cylinder_images[i].get_derivatives(all_matches[i]).Item1.ToString());
                        }
                        this.ResetSelf();
                        this.GetImageD();
                        LogPanel.Log($"image shown.");
                    }
                    return false;
                }
                if (verbose) LogPanel.Log($"multiplier = {multiplier}");
                double final_error = apply_change_all(multiplier);
                if (verbose)
                {
                    this.ResetSelf();
                    this.GetImageD();
                    LogPanel.Log($"done. final average error = {average_pixel_error(final_error)}");
                }
                return true;
                //System.Threading.Thread.Sleep(1000000000);
            }
            public void InitializeOnPlane()
            {
                Parallel.For(0, n, i => { image_providers[i].ResetSelf(); image_providers[i].GetImageD(); });
                this.ResetSelf();
                this.GetImageD();
                LogPanel.Log("searching features...");
                int progress = 0;
                Parallel.For(0, n, i => { int c = points_providers[i].GetPoints().Count; LogPanel.Log($"{c} features for image {i} ({System.Threading.Interlocked.Increment(ref progress)} of {n})"); });
                points = points_providers.Select(pp => pp.GetPoints().Select(ps => (ps as ImagePoint<PointsProviders.MSOP_DescriptorVector.Descriptor>)).ToList()).ToList();

                setup_image_params();
                this.ResetSelf();
                this.GetImageD();
                LogPanel.Log("ok");
            }
        }
    }
}
