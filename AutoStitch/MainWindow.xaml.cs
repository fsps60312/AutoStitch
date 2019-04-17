using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoStitch
{
    [Flags]
    public enum Direction { X=1, Y=2, All = X | Y }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SourceImagePanel source_image_panel;
        private void InitializeViews()
        {
            source_image_panel = new SourceImagePanel();
            IMatrixProvider
                mp_r = MatrixProviders.Filter.Red(source_image_panel),
                mp_hr = new MatrixProviders.HarrisDetectorResponse(mp_r),
                mp_g = MatrixProviders.Filter.Green(source_image_panel),
                mp_hg = new MatrixProviders.HarrisDetectorResponse(mp_g),
                mp_b = MatrixProviders.Filter.Blue(source_image_panel),
                mp_hb = new MatrixProviders.HarrisDetectorResponse(mp_b),
                mp_harris = new MatrixProviders.Add(mp_hr, mp_hg, mp_hb),
                mp_harris_debug = new MatrixProviders.Clamp(mp_harris, 0, 1e-3);
            IPointsProvider
                pp_harris_all = new PointsProviders.LocalMaximum(mp_harris, double.MinValue),
                pp_harris_filtered = new PointsProviders.LocalMaximum(mp_harris, 10 * 3.0 / (255.0 * 255.0)),
                pp_harris_refined=new PointsProviders.SubpixelRefinement(pp_harris_filtered,mp_harris);
            IImageD_Provider
                background=new ImageD_Providers.GrayImageD(mp_harris),
                mp_merge_all = new ImageD_Providers.PlotPoints(
                                    background,
                                    pp_harris_all
                                    ),
                mp_merge_filtered = new ImageD_Providers.PlotPoints(
                                    background,
                                    pp_harris_filtered
                                    ),
                mp_merge_refined = new ImageD_Providers.PlotPoints(
                                    background,
                                    pp_harris_refined
                                    );
            this.Height = 500;
            this.Width = 800;
            this.Content = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(2,GridUnitType.Star)},
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)}
                },
                Children =
                {
                    new ScrollViewer
                    {
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                        Content=new StackPanel
                        {
                            Orientation=Orientation.Vertical,
                            Children=
                            {
                                source_image_panel,
                                new Grid
                                {
                                    ColumnDefinitions=
                                    {
                                        new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)},
                                        new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)}
                                    },
                                    Children=
                                    {
                                        new Button{Content="Run"}.Set(async()=>{await Task.Run(()=>  {mp_merge_filtered.GetImageD();mp_merge_refined.GetImageD(); mp_merge_all.GetImageD(); });LogPanel.Log("done."); }).Set(0,0),
                                        new Button{Content="Reset"}.Set(async()=>{await Task.Run(()=>{mp_merge_filtered.Reset();mp_merge_refined.Reset();mp_merge_all.Reset(); });LogPanel.Log("reseted."); }).Set(0,1)
                                    }
                                },
                                //(new ImageViewer(mp_r)),
                                //(new ImageViewer(mp_hr)),
                                //(new ImageViewer(mp_g)),
                                //(new ImageViewer(mp_hg)),
                                //(new ImageViewer(mp_b)),
                                //(new ImageViewer(mp_hb)),
                                new ImageViewer(mp_harris),
                                new ImageViewer(new MatrixProviders.Clamp( mp_harris,10 * 3.0 / (255.0 * 255.0),double.MaxValue)),
                                new ImageViewer(mp_merge_all,false),
                                new ImageViewer(mp_merge_filtered,false),
                                new ImageViewer(mp_merge_refined,false)
                            }
                        }
                    }.Set(0,0),
                    new LogPanel().Set(0,1)
                }
            };
        }
        public MainWindow()
        {
            InitializeComponent();
            InitializeViews();
            LogPanel.Log("ok.");
        }
    }
}
