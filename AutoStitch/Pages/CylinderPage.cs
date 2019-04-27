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
            var global_viewer = new CorrectiveCylinderImages(source_image_panel.GetImages().Select(i => new ImageD_Providers.ImageD_Cache(i.ToImageD()) as IImageD_Provider).ToList(), 5000, 600);
            image_container.Content = new ImageViewer(global_viewer, false);
            await Task.Run(() =>
            {
                global_viewer.InitializeOnPlane();
                while(true)
                {
                    for(int i=0;i<100;i++)global_viewer.Refine();
                    global_viewer.ResetSelf();
                    global_viewer.GetImageD();
                }
            });
        }
        public CylinderPage()
        {
            InitializeViews();
        }
    }
}
