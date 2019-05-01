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
        const int pixel_width = 5000 / 2, pixel_height = 600 / 2;
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
                bool allow_perspective = false;
                bool allow_skew = false;
                for (DateTime time = DateTime.MinValue; ;)
                {
                    bool verbose = false;
                    if ((DateTime.Now - time).TotalSeconds > 10) verbose = true;
                    if (!global_viewer.Refine(allow_perspective, allow_skew, verbose))
                    {
                        if (allow_perspective)
                        {
                            if (allow_skew)
                            {
                                LogPanel.Log("done. generating image...");
                                global_viewer.ResetSelf();
                                global_viewer.GetImageD();
                                time = DateTime.Now;
                                LogPanel.Log("ok.");
                                break;
                            }
                            else
                            {
                                LogPanel.Log($"skew change on.");
                                global_viewer.ResetSelf();
                                global_viewer.GetImageD();
                                time = DateTime.Now;
                                LogPanel.Log("this is current result without skew changes.");
                                allow_skew = true;
                            }
                        }
                        else
                        {
                            LogPanel.Log($"perspective change on.");
                            global_viewer.ResetSelf();
                            global_viewer.GetImageD();
                            time = DateTime.Now;
                            LogPanel.Log("this is current result without perspective changes.");
                            allow_perspective = true;
                        }
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
