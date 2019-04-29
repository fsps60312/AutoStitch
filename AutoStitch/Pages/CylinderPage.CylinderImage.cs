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
            //  [   p       q       1   ][1]   [    xp+qy+1     ]
            //  h=((xαsinθ+yαcosθ+y')/(xp+qy+1))/sqrt(f*f+x*x)
            //  w=t+atan(((xαcosθ-yαsinθ+x')/(xp+qy+1))/f)
            //  params: α, θ, x', y', f, t
            //  have: pairwise (h,w), (H,W)
            //  minimize: β(h-H)^2+(w-W)^2
            //
            //  d: β(h-H)(dh-dH)+(w-W)(dw-dW)=0
            //  down_q=(xp+qy+1)*sqrt(f*f+x*x)
            //  down_f=(xp+qy+1)*f
            //  inside_tan=(xαcosθ-yαsinθ+x')/down_f
            //  one_x_2=1+inside_tan*inside_tan
            // 
            //  dh/dα: (xsinθ+ycosθ)/down_q
            //  dw/dα:  ((xcosθ-ysinθ)/down_f)/one_x_2
            //  dh/dθ:  (xαcosθ-yαsinθ)/down_q
            //  dw/dθ:  ((-xαsinθ-yαcosθ)/down_f)/one_x_2
            //  dh/dx': 0
            //  dw/dx': (1/down_f)/one_x_2
            //  dh/dy': 1/down_q
            //  dw/dy': 0
            //  dh/df:  ((xαsinθ+yαcosθ+y')/(xp+qy+1))*(-0.5/sqrt(f*f+x*x)^3)*(2*f)
            //  dw/df:  (-((xαcosθ-yαsinθ+x')/(xp+qy+1))/f^2)/one_x_2
            //  dh/dt:  0
            //  dw/dt:  1
            //  dh/dp:  (-((xαsinθ+yαcosθ+y')/sqrt(f*f+x*x))/(xp+qy+1)^2)*x
            //  dw/dp:  x*(-((xαcosθ-yαsinθ+x')/f)/(xp+qy+1)^2)/one_x_2
            //  dh/dq:  (-((xαsinθ+yαcosθ+y')/sqrt(f*f+x*x))/(xp+qy+1)^2)*y
            //  dw/dq:  y*(-((xαcosθ-yαsinθ+x')/f)/(xp+qy+1)^2)/one_x_2
            /// <summary>
            /// 
            /// </summary>
            /// <param name="beta"></param>
            /// <param name="matches">matched points of image positions</param>
            /// <param name="alpha"></param>
            /// <param name="theta"></param>
            /// <param name="dx"></param>
            /// <param name="dy"></param>
            /// <param name="df"></param>
            /// <param name="dt"></param>
            public void get_derivatives(double beta, List<Tuple<Tuple<double, double>, Tuple<double, double>, CylinderImage>> matches, out double alpha, out double theta, out double dx, out double dy, out double df, out double dt, out double dp, out double dq, out double total_error)
            {
                alpha = theta = dx = dy = df = dt = dp = dq = total_error = 0;
                foreach (var match in matches)
                {
                    (double x, double y) = (match.Item1.Item1 - 0.5 * width, match.Item1.Item2 - 0.5 * height);
                    (double w1, double h1) = image_point_to_camera(match.Item1.Item1, match.Item1.Item2);
                    (double w2, double h2) = match.Item3.image_point_to_camera(match.Item2.Item1, match.Item2.Item2);
                    if (w2 - w1 > Math.PI) w1 += 2.0 * Math.PI;
                    else if (w1 - w2 > Math.PI) w2 += 2.0 * Math.PI;
                    double pq_term = x * perspective_x + y * perspective_y + 1;
                    double fx_term = Math.Sqrt(focal_length * focal_length + x * x);
                    double h_term = x * Math.Sin(rotation_theta) + y * Math.Cos(rotation_theta);
                    double dh_term = x * Math.Cos(rotation_theta) - y * Math.Sin(rotation_theta);
                    double w_term = x * Math.Cos(rotation_theta) - y * Math.Sin(rotation_theta);
                    double dw_term = -x * Math.Sin(rotation_theta) - y * Math.Cos(rotation_theta);
                    //double down_q = pq_term * fx_term;
                    //double down_f = pq_term * focal_length;
                    double inside_tan = (scalar_alpha * dw_term + displace_x) / pq_term / focal_length;
                    double one_x_2 = 1 + inside_tan * inside_tan;

                    alpha += beta * (h1 - h2) * (h_term / pq_term / fx_term) +
                        (w1 - w2) * ((w_term / pq_term / focal_length) / one_x_2);
                    theta += beta * (h1 - h2) * ((scalar_alpha * dh_term) / pq_term / fx_term) +
                        (w1 - w2) * (((scalar_alpha * dw_term) / pq_term / focal_length) / one_x_2);
                    dx += (w1 - w2) * ((1 / pq_term / focal_length) / one_x_2);
                    dy += beta * (h1 - h2) * (1 / pq_term / fx_term);
                    df += beta * (h1 - h2) * (
                        ((scalar_alpha * h_term + displace_y) / pq_term) *
                        (-0.5 * Math.Pow(focal_length * focal_length + x * x, -1.5)) *
                        (2 * focal_length)) +
                        (w1 - w2) * ((-(scalar_alpha * w_term + displace_x) / (pq_term * focal_length * focal_length)) / one_x_2);
                    dt += (w1 - w2);
                    //double dh_perspective = -((scalar_alpha * h_term + displace_y) / fx_term) / (pq_term * pq_term);
                    //double dw_perspective = (-(scalar_alpha * w_term + displace_x) / (focal_length * pq_term * pq_term)) / one_x_2;
                    //dp += beta * (h1 - h2) * (dh_perspective * x) +
                    //    (w1 - w2) * (dw_perspective * x);
                    //dq += beta * (h1 - h2) * (dh_perspective * y) +
                    //    (w1 - w2) * (dw_perspective * y);
                    total_error += beta * (h1 - h2) * (h1 - h2) + (w1 - w2) * (w1 - w2);
                }
                alpha *= 2; theta *= 2; dx *= 2; dy *= 2; df *= 2; dt *= 2; dp *= 2; dq *= 2;
                // regularization
                const double regularization = 1e-5;
                dp -= regularization * 2 * perspective_x;
                dq -= regularization * 2 * perspective_y;
                total_error += regularization + (perspective_x * perspective_x + perspective_y * perspective_y);
            }
            IImageD_Provider image_provider;
            public double center_direction;
            public double focal_length;
            public double rotation_theta = 0, scalar_alpha = 1, displace_x = 0, displace_y = 0, perspective_x = 0, perspective_y = 0;
            private double[,]get_matrix()
            {
                return new double[3, 3]
                {
                    {scalar_alpha*Math.Cos(rotation_theta),-scalar_alpha*Math.Sin(rotation_theta),displace_x },
                    {scalar_alpha*Math.Sin(rotation_theta),scalar_alpha*Math.Cos(rotation_theta),displace_y },
                    {perspective_x,perspective_y,1 }
                };
            }
            private static double[,] inverse(double[,] a)
            {
                System.Diagnostics.Trace.Assert(a.GetLength(0) == 3 && a.GetLength(1) == 3);
                double[,] ans = new double[3, 3]
                {
                    { a[1,1]*a[2,2]-a[1,2]*a[2,1], a[0,2]*a[2,1]-a[0,1]*a[2,2], a[0,1]*a[1,2]-a[0,2]*a[1,1] },
                    { a[1,2]*a[2,0]-a[1,0]*a[2,2], a[0,0]*a[2,2]-a[0,2]*a[2,0], a[0,2]*a[1,0]-a[0,0]*a[1,2] },
                    { a[1,0]*a[2,1]-a[1,1]*a[2,0], a[0,1]*a[2,0]-a[0,0]*a[2,1], a[0,0]*a[1,1]-a[0,1]*a[1,0] }
                };
                double det = 0;
                for (int i = 0; i < 3; i++) det += a[0, i] * a[1, (i + 1) % 3] * a[2, (i + 2) % 3] - a[0, i] * a[1, (i + 2) % 3] * a[2, (i + 1) % 3];
                for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) ans[i, j] /= det;
                return ans;
            }
            private double[] recover_z(double[] a)
            {
                System.Diagnostics.Trace.Assert(a.Length == 2);
                double[,] m = inverse(get_matrix());
                // (m[2,0]*a[0]+m[2,1]*a[1])*z+m[2,2]*z==1
                double z = 1.0 / (m[2, 0] * a[0] + m[2, 1] * a[1] + m[2, 2]);
                return new double[3] { a[0] * z, a[1] * z, z };
            }
            private static double[] multiply(double[,] a, double[] b)
            {
                System.Diagnostics.Trace.Assert(a.GetLength(0) == 3 && a.GetLength(1) == 3 && b.Length == 3);
                double[] ans = new double[3];
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        ans[i] += a[i, j] * b[j];
                    }
                }
                return ans;
            }
            public (double, double) image_point_to_camera(double x, double y)
            {
                double x_backup = x, y_backup = y;
                x -= 0.5 * width; y -= 0.5 * height;
                double[] a = new double[3] { x, y, 1 };
                a = multiply(get_matrix(), a);
                (x, y) = (a[0] / a[2], a[1] / a[2]);
                double w = center_direction + Math.Atan(x / focal_length);
                double h = y / Math.Sqrt(x * x + focal_length * focal_length);
                w %= 2.0 * Math.PI;
                if (w < 0) w += 2.0 * Math.PI;
                {
                    (x, y) = camera_to_image_point(w, h);
                    double error = Math.Sqrt(Math.Pow(x - x_backup, 2) + Math.Pow(y - y_backup, 2));
                    System.Diagnostics.Trace.Assert(error < 1e-8);
                    //if (error > 1e-9) LogPanel.Log($"error = {error}, x: {x_backup} → {x}, y: {y_backup} → {y}");
                }
                return (w, h);
            }
            public (double, double) camera_to_image_point(double direction, double h)
            {
                double angle_diff = (direction - center_direction) % (2.0 * Math.PI);
                if (angle_diff < 0) angle_diff += 2.0 * Math.PI;
                if (0.5 * Math.PI <= angle_diff && angle_diff <= 1.5 * Math.PI) return (double.NaN, double.NaN);
                MyImageD image = image_provider.GetImageD();
                double x = Math.Tan(direction - center_direction) * focal_length;
                double y = h * (Math.Sqrt(x * x + focal_length * focal_length));
                double[] a = recover_z(new[] { x, y });
                a = multiply(inverse(get_matrix()), a);
                System.Diagnostics.Trace.Assert(Math.Abs(a[2] - 1) < 1e-8);
                //(x, y) = (x - displace_x, y - displace_y);
                //(x, y) = ((x * Math.Cos(-rotation_theta) - y * Math.Sin(-rotation_theta)) / scalar_alpha, (x * Math.Sin(-rotation_theta) + y * Math.Cos(-rotation_theta)) / scalar_alpha);
                (x, y) = (a[0], a[1]);
                return (x + 0.5 * width, y + 0.5 * height);
            }
            public int height { get { return image_provider.GetImageD().height; } }
            public int width { get { return image_provider.GetImageD().width; } }
            public CylinderImage(IImageD_Provider image_provider, double center_direction, double focal_length)
            {
                this.image_provider = image_provider;
                this.center_direction = center_direction;
                this.focal_length = focal_length;
            }
            public bool sample_pixel(double direction, double h, out double r, out double g, out double b)
            {
                // h = y*(r/sqrt(x^2+f^2))
                // a = center_direction+atan(x/f)
                // r = 1, f fixed, for each "h, a", find "x, y"\
                MyImageD image = image_provider.GetImageD();
                (double x, double y) = camera_to_image_point(direction, h);
                return image.sample(x, y, out r, out g, out b);
            }
        }
    }
}
