using System;
using System.Collections.Generic;

namespace AutoStitch.Pages
{
    partial class CylinderPage
    {
        class CylinderImage
        {
            //  [   αcosθ   -αsinθ  x'  ][x]   [xαcosθ-yαsinθ+x']
            //  [   αsinθ   αcosθ   y'  ][y] = [xαsinθ+yαcosθ+y']
            //  [   0       0       1   ][1]   [       1        ]
            //  h=(xαsinθ+yαcosθ+y')/f
            //  w=t+atan((xαcosθ-yαsinθ+x')/f)
            //  params: α, θ, x', y', f, t
            //  have: pairwise (h,w), (H,W)
            //  minimize: β(h-H)^2+(w-W)^2
            //
            //  d: β(h-H)(dh-dH)+(w-W)(dw-dW)=0
            //  dh/dα:  (xsinθ+ycosθ)/f
            //  dw/dα:  ((xcosθ-ysinθ)/f)/(1+((xαcosθ-yαsinθ+x')/f)^2)
            //  dh/dθ:  (xαcosθ-yαsinθ)/f
            //  dw/dθ:  ((-xαsinθ-yαcosθ)/f)/(1+((xαcosθ-yαsinθ+x')/f)^2)
            //  dh/dx': 0
            //  dw/dx': (1/f)/(1+((xαcosθ-yαsinθ+x')/f)^2)
            //  dh/dy': 1/f
            //  dw/dy': 0
            //  dh/df:  -(xαsinθ+yαcosθ+y')/f^2
            //  dw/df:  (-(xαcosθ-yαsinθ+x')/f^2)/(1+((xαcosθ-yαsinθ+x')/f)^2)
            //  dh/dt:  0
            //  dw/dt:  1
            public void get_derivatives(double beta, List<Tuple<Tuple<double, double>, Tuple<double, double>,CylinderImage>>matches,out double alpha,out double theta,out double dx,out double dy,out double df,out double dt)
            {
                alpha = theta = dx = dy = df = dt = 0;
                foreach (var match in matches)
                {
                    (double x, double y) = (match.Item1.Item1, match.Item1.Item2);
                    (double w1, double h1) = image_point_to_camera(x, y);
                    (double w2, double h2) = match.Item3.image_point_to_camera(match.Item2.Item1, match.Item2.Item2);
                    double inside_tan = (x * scalar_alpha * Math.Cos(rotation_theta) - y * scalar_alpha * Math.Sin(rotation_theta) + displace_x) / focal_length;
                    double one_x_2 = 1 + inside_tan * inside_tan;

                    alpha += beta * (h1 - h2) * ((x * Math.Sin(rotation_theta) + y * Math.Cos(rotation_theta)) / focal_length) +
                        (w1 - w2) * (((x * Math.Cos(rotation_theta) - y * Math.Sin(rotation_theta)) / focal_length) / one_x_2);
                    theta += beta * (h1 - h2) * ((x * scalar_alpha * Math.Cos(rotation_theta) - y * scalar_alpha * Math.Sin(rotation_theta)) / focal_length) +
                        (w1 - w2) * (((-x * scalar_alpha * Math.Sin(rotation_theta) - y * scalar_alpha * Math.Cos(rotation_theta)) / focal_length) / one_x_2);
                    dx += (w1 - w2) * ((1 / focal_length) / one_x_2);
                    dy += beta * (h1 - h2) * (1 / focal_length);
                    df += beta * (h1 - h2) * (-(x * scalar_alpha * Math.Sin(rotation_theta) + y * scalar_alpha * Math.Cos(rotation_theta) + displace_y) / (focal_length * focal_length)) +
                        (w1 - w2) * ((-(x * scalar_alpha * Math.Cos(rotation_theta) - y * scalar_alpha * Math.Sin(rotation_theta)) / (focal_length * focal_length)) / one_x_2);
                    dt += (w1 - w2);
                }
            }
            IImageD_Provider image_provider;
            public double center_direction;
            public double focal_length;
            public double rotation_theta = 0, scalar_alpha = 1, displace_x = 0, displace_y = 0;
            public (double, double) image_point_to_camera(double x, double y)
            {
                x -= 0.5 * width;y -= 0.5 * height;
                return (
                    center_direction + Math.Atan((x * scalar_alpha * Math.Cos(rotation_theta) - y * scalar_alpha * Math.Sin(rotation_theta) + displace_x) / focal_length),
                    (x * scalar_alpha * Math.Sin(rotation_theta) + y * scalar_alpha * Math.Cos(rotation_theta) + displace_y) / focal_length);

            }
            public double height { get { return image_provider.GetImageD().height; } }
            public double width { get { return image_provider.GetImageD().width; } }
            public CylinderImage(IImageD_Provider image_provider,double center_direction,double focal_length)
            {
                this.image_provider = image_provider;
                this.center_direction = center_direction;
                this.focal_length = focal_length;
            }
            public bool sample_pixel(double direction, double cylinder_radius, double h,out double r,out double g,out double b)
            {
                // h = y*(r/sqrt(x^2+f^2))
                // a = center_direction+atan(x/f)
                // r = 1, f fixed, for each "h, a", find "x, y"
                double angle_diff = (direction - center_direction) % (2.0 * Math.PI);
                if (angle_diff < 0) angle_diff += 2.0 * Math.PI;
                if (0.5 * Math.PI <= angle_diff && angle_diff <= 1.5 * Math.PI) { r = g = b = 0; return false; }
                MyImageD image = image_provider.GetImageD();
                double x = Math.Tan(direction - center_direction) * focal_length;
                double y = h * (Math.Sqrt(x * x + focal_length * focal_length) / cylinder_radius);
                x += 0.5 * width;y += 0.5 * height;
                (x, y) = ((x * Math.Cos(-rotation_theta) - y * Math.Sin(-rotation_theta)) / scalar_alpha, (x * Math.Sin(-rotation_theta) + y * Math.Cos(-rotation_theta)) / scalar_alpha);
                return image.sample(x, y, out r, out g, out b);
            }
        }
    }
}
