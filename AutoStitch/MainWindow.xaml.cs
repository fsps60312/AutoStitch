﻿using System;
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
            IImageD_Provider main_image = source_image_panel.GetImageD_Provider(0);
            IImageD_Provider side_image = source_image_panel.GetImageD_Provider(1);
            IPointsProvider main_feature_points_provider = new PointsProviders.MSOP_DescriptorVector(new PointsProviders.MultiScaleHarrisCornerDetector(main_image), new MatrixProviders.GrayScale(main_image));
            IPointsProvider side_feature_points_provider = new PointsProviders.MSOP_DescriptorVector(new PointsProviders.MultiScaleHarrisCornerDetector(side_image), new MatrixProviders.GrayScale(side_image));
            IImageD_Provider mp_merge_refined = new ImageD_Providers.PlotPoints(
                                    new ImageD_Providers.GrayImageD(new MatrixProviders.GrayScale(main_image)),
                                    main_feature_points_provider
                                    );
            Controls.PointSelect main_features = new Controls.PointSelect(main_image, main_feature_points_provider);
            Controls.PointSelect side_features = new Controls.PointSelect(side_image, side_feature_points_provider);
            main_features.PointSelected += selected_point =>
              {
                  var main_descriptor = (PointsProviders.MSOP_DescriptorVector.Descriptor)selected_point.content;
                  ImagePoint nearst = null;
                  double first_min = double.MaxValue, second_min = double.MaxValue;
                  foreach (var p in side_features.points)
                  {
                      double dis = main_descriptor.difference((PointsProviders.MSOP_DescriptorVector.Descriptor)p.content);
                      if (dis < first_min)
                      {
                          second_min = first_min;
                          first_min = dis;
                          nearst = p;
                      }
                      else if (dis < second_min) second_min = dis;
                  }
                  if (first_min / second_min < 0.8)
                  {
                      LogPanel.Log($"nearst feature diff = {main_descriptor.difference((PointsProviders.MSOP_DescriptorVector.Descriptor)nearst.content)}");
                      side_features.ShowPoint(nearst.x, nearst.y);
                  }
                  else
                  {
                      LogPanel.Log($"nearst feature too similar, no match!");
                  }
              };
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
                                        new Button{Content="Run"}.Set(async()=>{await Task.Run(()=>  {mp_merge_refined.GetImageD(); });LogPanel.Log("done."); }).Set(0,0),
                                        new Button{Content="Reset"}.Set(async()=>{await Task.Run(()=>{mp_merge_refined.Reset(); });LogPanel.Log("reseted."); }).Set(0,1)
                                    }
                                },
                                //(new ImageViewer(mp_r)),
                                //(new ImageViewer(mp_hr)),
                                //(new ImageViewer(mp_g)),
                                //(new ImageViewer(mp_hg)),
                                //(new ImageViewer(mp_b)),
                                //(new ImageViewer(mp_hb)),
                                new ImageViewer(mp_merge_refined,false),
                                //new ImageViewer(main_image,false),
                                //new Controls.PointSelect(source_image_panel,new PointsProviders.PointsCache(new List<ImagePoint>{ new ImagePoint(10,10,1),new ImagePoint(100,50,5) }))
                                main_features,
                                side_features
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
