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
            int kase_self = System.Threading.Interlocked.Increment(ref kase);
            var global_viewer = new CorrectiveCylinderImages(source_image_panel.GetImages().Select(i => new ImageD_Providers.ImageD_Cache(i.ToImageD()) as IImageD_Provider).ToList(), pixel_width, pixel_height);
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
                (int,int) prev_stage = (-1,-1);
                int stage = 0, cached_stage = 0;
                for (DateTime time = DateTime.MinValue; ;)
                {
                    if (prev_stage != (stage,cached_stage))
                    {
                        prev_stage = (stage, cached_stage);
                        LogPanel.Log($"refine #{global_viewer.refine_count+1}: stage: {stage} ← {cached_stage} / 7");
                    }
                    bool verbose = false;
                    if ((DateTime.Now - time).TotalSeconds > 30) verbose = true;
                    bool result = global_viewer.Refine(stage > 0, stage > 1, stage > 2, verbose);
                    if (!result)
                    {
                        if (stage > 0) stage--;
                        else stage = cached_stage = cached_stage + 1;
                    }
                    else if (stage < cached_stage) cached_stage = stage;
                    if (stage == 4)
                    {
                        System.Diagnostics.Trace.Assert(stage == 7);
                        LogPanel.Log("done. generating image...");
                        System.Diagnostics.Trace.Assert(!global_viewer.Refine(true, true, true, true));
                        LogPanel.Log("ok.");
                        return;
                    }
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
