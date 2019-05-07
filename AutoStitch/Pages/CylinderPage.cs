using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace AutoStitch.Pages
{
    partial class CylinderPage:ContentControl
    {
        const int pixel_width = 5000 , pixel_height = 600;
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
            //for (int seed = 0; seed < 100; seed++)
            //{
            //    int n = Utils.Rand(10, 100);
            //    double[,] m = Utils.MatrixRandom(n, n,-10,10, seed);
            //    double[,] i = Utils.MatrixInverse(m);
            //    double[,] I = Utils.MatrixIdentity(n);
            //    double[,] p = Utils.MatrixProduct(m, i);
            //    System.Diagnostics.Trace.Assert(Utils.MatrixAreEqual(p, I, 1.0E-8));
            //}
            int kase_self = System.Threading.Interlocked.Increment(ref kase);
            var image_providers = source_image_panel.GetImages().Select(i => new ImageD_Providers.ImageD_Cache(i.ToImageD()) as IImageD_Provider).ToList();
            while (image_providers.Any(p => { var i = p.GetImageD(); return i.width * i.height > 1000000; }))
            {
                LogPanel.Log($"scaling down...");
                Parallel.For(0, image_providers.Count, i =>
                  {
                      var p = image_providers[i];
                      image_providers[i] = new ImageD_Providers.ImageD_Cache(new ImageD_Providers.Scale(new ImageD_Providers.GaussianBlur(p, 0.5), 0.5).GetImageD());
                  });
            }
            var global_viewer = new CorrectiveCylinderImages(image_providers, pixel_width, pixel_height);
            image_container.Content = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = new ImageViewer(global_viewer, false)
            };
            await Task.Run(() =>
            {
                global_viewer.InitializeOnPlane();
                // stage1: false false false
                // stage2: true false false
                // stage3: false false false (roll back to stage1)
                // stage4: true true false
                // stage5: true false false (roll back to stage2)
                // stage6: false false false (roll back to stage1)
                // stage7: ...
                (int,int) prev_freedom = (-1,-1);
                int freedom = 1, cached_freedom = 1;
                for (DateTime time = DateTime.MinValue; ;)
                {
                    //freedom = cached_freedom = 10;
                    if (prev_freedom != (freedom,cached_freedom))
                    {
                        prev_freedom = (freedom, cached_freedom);
                        LogPanel.Log($"refine #{global_viewer.refine_count+1}: freedom: {freedom} ← {cached_freedom} / {CorrectiveCylinderImages.maximum_freedom}");
                    }
                    bool verbose = false;
                    if ((DateTime.Now - time).TotalSeconds > 30) verbose = true;
                    bool result = global_viewer.Refine(freedom + (Utils.RandDouble() < 0.01 ? (Utils.RandDouble() < 0.5 ? 1 : 2) : 0), verbose);
                    if (!result)
                    {
                        if (freedom > 1) freedom--;
                        else freedom = cached_freedom = cached_freedom + 1;
                    }
                    else if (freedom < cached_freedom) cached_freedom = freedom;
                    if (freedom > CorrectiveCylinderImages.maximum_freedom)
                    {
                        //System.Diagnostics.Trace.Assert(freedom == 7);
                        LogPanel.Log("done. generating image...");
                        System.Diagnostics.Trace.Assert(!global_viewer.Refine(CorrectiveCylinderImages.maximum_freedom, true));
                        for (int i = 0; i < global_viewer.cylinder_images.Count; i++)
                        {
                            LogPanel.Log($"params of image[{i}]:");
                            LogPanel.Log(global_viewer.cylinder_images[i].transform.ToString());
                        }
                        LogPanel.Log("ok.");
                        return;
                    }
                    //if (verbose)
                    //{
                    //    for (int i = 1; i <= CorrectiveCylinderImages.maximum_freedom; i++)
                    //    {
                    //        if (!global_viewer.Test(i, false)) LogPanel.Log($"problem entry: {i}");
                    //        else LogPanel.Log($"entry {i} seems ok.");
                    //    }
                    //}
                    if (verbose) time = DateTime.Now;
                }
            });
        }
        public CylinderPage()
        {
            InitializeViews();
        }
    }
}
