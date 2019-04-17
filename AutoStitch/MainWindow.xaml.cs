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
            MatrixProvider mp_r, mp_g, mp_b, mp_hr, mp_hg, mp_hb, mp_harris = null;
            ImageD_Provider mp_merge=null;
            mp_r = MatrixProviders.Filter.Red(source_image_panel);
            mp_hr = new MatrixProviders.HarrisDetectorResponse(mp_r);
            mp_g = MatrixProviders.Filter.Green(source_image_panel);
            mp_hg = new MatrixProviders.HarrisDetectorResponse(mp_g);
            mp_b = MatrixProviders.Filter.Blue(source_image_panel);
            mp_hb = new MatrixProviders.HarrisDetectorResponse(mp_b);
            mp_harris = new MatrixProviders.Clamp(new MatrixProviders.Add(mp_hr, mp_hg, mp_hb), 0, 1e-3);
            mp_merge = new ImageD_Providers.Blend(
                                    new ImageD_Providers.GrayImageD(new MatrixProviders.GrayScale(source_image_panel)),
                                    new ImageD_Providers.HeatImageD(mp_harris)
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
                                        new Button{Content="Run"}.Set(async()=>{await Task.Run(()=>  {mp_merge.GetImageD(); });LogPanel.Log("done."); }).Set(0,0),
                                        new Button{Content="Reset"}.Set(async()=>{await Task.Run(()=>mp_merge.Reset());LogPanel.Log("reseted."); }).Set(0,1)
                                    }
                                },
                                //(new ImageViewer(mp_r)),
                                //(new ImageViewer(mp_hr)),
                                //(new ImageViewer(mp_g)),
                                //(new ImageViewer(mp_hg)),
                                //(new ImageViewer(mp_b)),
                                //(new ImageViewer(mp_hb)),
                                //(new ImageViewer(mp_harris)),
                                (new ImageViewer(mp_merge,false))
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
