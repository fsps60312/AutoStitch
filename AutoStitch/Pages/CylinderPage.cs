using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace AutoStitch.Pages
{
    class CylinderPage:ContentControl
    {
        class Cylinder
        { // cylinder radius = 1
            List<CylinderImage> images = new List<CylinderImage>();
            //bool hit_test(double h,double )
        }
        class CylinderImage
        {
            IImageD_Provider image_provider;
            double center_direction;
            double focal_length;
            public CylinderImage(IImageD_Provider image_provider,double center_direction,double focal_length)
            {
                this.image_provider = image_provider;
                this.center_direction = center_direction;
                this.focal_length = focal_length;
            }
            bool sample_pixel(double direction, double cylinder_radius, double h,out double r,out double g,out double b)
            {
                // h = y*(r/sqrt(x^2+f^2))
                // a = center_direction+atan(x/f)
                // r = 1, f fixed, for each "h, a", find "x, y"
                MyImageD image = image_provider.GetImageD();
                double x = Math.Tan(direction - center_direction) * focal_length;
                double y = h * (Math.Sqrt(x * x + focal_length * focal_length) / cylinder_radius);
                return image.sample(x, y, out r, out g, out b);
            }
        }
        SourceImagePanel source_image_panel = new SourceImagePanel(false);
        Image image_viewer = new Image();
        private void InitializeViews()
        {
            this.Content = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)},
                    new ColumnDefinition{Width=new GridLength(2,GridUnitType.Star)}
                },
                Children =
                {
                    source_image_panel.Set(0,0),
                    image_viewer.Set(1,0)
                }
            };
        }
        public CylinderPage()
        {
            InitializeViews();
        }
    }
}
