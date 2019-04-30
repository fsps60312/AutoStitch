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
            public (Transform, double) get_derivatives(double beta, List<Tuple<Tuple<double, double>, Tuple<double, double>, CylinderImage>> matches)
            {
                Transform derivative = new Transform(null, 0, 0, 0, 0, 0, 0, 0, 0);
                double total_error = 0;
                foreach (var match in matches)
                {
                    //  h=((xαsinθ+yαcosθ+y')/(xp+qy+1))/sqrt(f*f+x*x)
                    //  w=t+atan(((xαcosθ-yαsinθ+x')/(xp+qy+1))/f)
                    (double x, double y) = (match.Item1.Item1 - 0.5 * width, match.Item1.Item2 - 0.5 * height);
                    double pq_term = x * perspective_x + y * perspective_y + 1;
                    double fx_term = Math.Sqrt(focal_length * focal_length + x * x);
                    double h_term = x * Math.Sin(rotation_theta) + y * Math.Cos(rotation_theta);
                    double dh_term = x * Math.Cos(rotation_theta) - y * Math.Sin(rotation_theta);
                    double w_term = x * Math.Cos(rotation_theta) - y * Math.Sin(rotation_theta);
                    double dw_term = -x * Math.Sin(rotation_theta) - y * Math.Cos(rotation_theta);
                    double inside_tan = (scalar_alpha * w_term + displace_x) / pq_term / focal_length;
                    double one_x_2 = 1 + inside_tan * inside_tan;
                    (double w1, double h1) = image_point_to_camera(match.Item1.Item1, match.Item1.Item2);
                    (double w2, double h2) = match.Item3.image_point_to_camera(match.Item2.Item1, match.Item2.Item2);
                    if (w2 - w1 > Math.PI) w1 += 2.0 * Math.PI;
                    else if (w1 - w2 > Math.PI) w2 += 2.0 * Math.PI;

                    derivative.scalar_alpha += (w1 - w2) * ((w_term / pq_term / focal_length) / one_x_2) +
                        beta * (h1 - h2) * (h_term / pq_term / fx_term);
                    derivative.rotation_theta += (w1 - w2) * (((scalar_alpha * dw_term) / pq_term / focal_length) / one_x_2) +
                        beta * (h1 - h2) * ((scalar_alpha * dh_term) / pq_term / fx_term);
                    derivative.displace_x += (w1 - w2) * ((1 / pq_term / focal_length) / one_x_2);
                    derivative.displace_y += beta * (h1 - h2) * (1 / pq_term / fx_term);
                    derivative.focal_length += (w1 - w2) * ((-(scalar_alpha * w_term + displace_x) / (pq_term * focal_length * focal_length)) / one_x_2) +
                         beta * (h1 - h2) * (
                         ((scalar_alpha * h_term + displace_y) / pq_term) *
                         (-0.5 * Math.Pow(focal_length * focal_length + x * x, -1.5)) *
                         (2 * focal_length));
                    derivative.center_direction += (w1 - w2);
                    double dw_perspective = (-(scalar_alpha * w_term + displace_x) / (focal_length * pq_term * pq_term)) / one_x_2;
                    double dh_perspective = -(scalar_alpha * h_term + displace_y) / (fx_term * pq_term * pq_term);
                    derivative.perspective_x += (w1 - w2) * (dw_perspective * x) +
                         beta * (h1 - h2) * (dh_perspective * x);
                    derivative.perspective_y += (w1 - w2) * (dw_perspective * y) +
                         beta * (h1 - h2) * (dh_perspective * y);
                    total_error += beta * (h1 - h2) * (h1 - h2) + (w1 - w2) * (w1 - w2);
                }
                derivative *= 2;
                // regularization
                const double regularization = 1e-5;
                derivative.perspective_x += regularization * 2 * perspective_x;
                derivative.perspective_y += regularization * 2 * perspective_y;
                total_error += regularization * (perspective_x * perspective_x + perspective_y * perspective_y);
                return (derivative, total_error);
            }
            IImageD_Provider image_provider;
            public class Transform
            {
                public double center_direction;
                public double focal_length;
                public double rotation_theta = 0, scalar_alpha = 1, displace_x = 0, displace_y = 0, perspective_x = 0, perspective_y = 0;
                CylinderImage parent;
                public Transform Copy()
                {
                    return new Transform(parent)
                    {
                        center_direction = center_direction,
                        focal_length = focal_length,
                        rotation_theta = rotation_theta,
                        scalar_alpha = scalar_alpha,
                        displace_x = displace_x,
                        displace_y = displace_y,
                        perspective_x = perspective_x,
                        perspective_y = perspective_y
                    };
                }
                public Transform(CylinderImage parent) { this.parent = parent; }
                public Transform(CylinderImage parent,double center_direction,double focal_length, double rotation_theta ,double scalar_alpha,double displace_x,double displace_y ,double perspective_x ,double perspective_y):this(parent)
                {
                    this.center_direction = center_direction;
                    this.focal_length = focal_length;
                    this.rotation_theta = rotation_theta;
                    this.scalar_alpha = scalar_alpha;
                    this.displace_x = displace_x;
                    this.displace_y = displace_y;
                    this.perspective_x = perspective_x;
                    this.perspective_y = perspective_y;
                }
                private double[,] get_matrix()
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
                private double[] recover_z(double xp, double h)
                {
                    // a[0]/z
                    // x' = Math.Tan(direction - center_direction) * focal_length;
                    // a[1]/z
                    // y' = h * Math.Sqrt(x * x + focal_length * focal_length);
                    // (m[2,0]*x'+m[2,1]*y'+m[2,2])*z==1
                    // x=(m[0,0]*x'+m[0,1]*y'+m[0,2])*z
                    // y' = h * Math.Sqrt((m[0,0]*x'+m[0,1]*y'+m[0,2])^2z^2 + f^2)
                    // y'^2 = h^2 * ((m[0,0]*x'+m[0,1]*y'+m[0,2])^2z^2 + f^2)
                    // y'^2 = (m[0,0]*x'+m[0,1]*y'+m[0,2])^2z^2h^2 + f^2h^2
                    // y'^2 - f^2h^2 = (m[0,0]*x'+m[0,1]*y'+m[0,2])^2z^2h^2
                    // (y'^2 - f^2h^2)/((m[0,0]*x'+m[0,1]*y'+m[0,2])^2h^2) = z^2
                    // sqrt((y'^2 - f^2h^2)/((m[0,0]*x'+m[0,1]*y'+m[0,2])^2h^2)) = z
                    // sqrt(y'^2 - f^2h^2)/((m[0,0]*x'+m[0,1]*y'+m[0,2])h) = z
                    // (m[2,0]*x'+m[2,1]*y'+m[2,2])*z==1
                    // (m[2,0]*x'+m[2,1]*y'+m[2,2])*(sqrt(y'^2 - f^2h^2)/((m[0,0]*x'+m[0,1]*y'+m[0,2])h))==1
                    // (m[2,0]*x'+m[2,1]*y'+m[2,2])*sqrt(y'^2 - f^2h^2)==(m[0,0]*x'+m[0,1]*y'+m[0,2])h
                    // (m[2,0]*x'+m[2,1]*y'+m[2,2])^2(y'^2 - f^2h^2)==(m[0,0]*x'+m[0,1]*y'+m[0,2])^2h^2
                    double[,] m = inverse(get_matrix());
                    double x = xp;
                    double yp_nxt = h * Math.Sqrt(x * x + focal_length * focal_length), yp;
                    double z;
                    int num_iterations = 0;
                    do
                    {
                        yp = yp_nxt;
                        z = 1.0 / (m[2, 0] * xp + m[2, 1] * yp + m[2, 2]);
                        if (z < 1e-9) return null;
                        x = (m[0, 0] * xp + m[0, 1] * yp + m[0, 2]) * z;
                        yp_nxt = h * Math.Sqrt(x * x + focal_length * focal_length);
                        ++num_iterations;
                        if (num_iterations > 10 && (x < -parent.width || x > parent.width * 2)) return null;// x out of image's range
                        if (num_iterations >= 1000) throw new Exception($"timeout iterations={num_iterations}\nxp={xp}\nh={h}\nz={z}\nyp: {yp} → {yp_nxt}");
                    }
                    while (Math.Abs(yp - yp_nxt) > 1e-9);
                    //while (yp != yp_nxt);
                    yp = yp_nxt;
                    return new double[3] { xp * z, yp * z, z };
                    //System.Diagnostics.Trace.Assert(a.Length == 2);
                    //double z = 1.0 / (m[2, 0] * a[0] + m[2, 1] * a[1] + m[2, 2]);
                    //return new double[3] { a[0] * z, a[1] * z, z };
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
                public (double,double)transform((double,double) p)
                {
                    (double x, double y) = p;
                    double[] a = multiply(get_matrix(), new[] { x, y, 1 });
                    (double w, double h) = (Utils.Mod2PI(center_direction + Math.Atan((a[0] / a[2]) / focal_length)),
                            (a[1] / a[2]) / Math.Sqrt(x * x + focal_length * focal_length));
                    return (w, h);
                }
                public (double,double)untransform((double,double)p)
                {
                    (double w, double h) = p;
                    double angle_diff = Utils.Mod2PI(w - center_direction);
                    if (0.5 * Math.PI <= angle_diff && angle_diff <= 1.5 * Math.PI) return (double.NaN, double.NaN);
                    double xp = Math.Tan(angle_diff) * focal_length;
                    double[] a;
                    try { a = recover_z(xp, h); }
                    catch (Exception error) { throw new Exception(error.Message + $"\nangle_diff={angle_diff}\nfocal_length={focal_length}"); }
                    if (a == null) return (double.NaN, double.NaN);
                    a = multiply(inverse(get_matrix()), a);
                    System.Diagnostics.Trace.Assert(Math.Abs(a[2] - 1) < 1e-8);
                    return (a[0], a[1]);
                }
                public double square_sum()
                {
                    return center_direction * center_direction +
                        focal_length * focal_length +
                        rotation_theta * rotation_theta +
                        scalar_alpha * scalar_alpha +
                        displace_x * displace_x +
                        displace_y * displace_y +
                        perspective_x * perspective_x +
                        perspective_y * perspective_y;
                }
                public static Transform operator+(Transform a,Transform b)
                {
                    return new Transform(a.parent,
                        a.center_direction + b.center_direction,
                        a.focal_length + b.focal_length,
                        a.rotation_theta + b.rotation_theta,
                        a.scalar_alpha + b.scalar_alpha,
                        a.displace_x + b.displace_x,
                        a.displace_y + b.displace_y,
                        a.perspective_x + b.perspective_x,
                        a.perspective_y + b.perspective_y);
                }
                public static Transform operator*(Transform a,double b)
                {
                    return new Transform(a.parent,
                        a.center_direction * b,
                        a.focal_length * b,
                        a.rotation_theta * b,
                        a.scalar_alpha * b,
                        a.displace_x * b,
                        a.displace_y * b,
                        a.perspective_x * b,
                        a.perspective_y * b);
                }
            }
            public Transform transform;
            private Transform saved_transform = null;
            public double center_direction { get { return transform.center_direction; } }
            public double focal_length { get { return transform.focal_length; } }
            public double scalar_alpha { get { return transform.scalar_alpha; } }
            public double rotation_theta { get { return transform.rotation_theta; } }
            public double perspective_x { get { return transform.perspective_x; } }
            public double perspective_y { get { return transform.perspective_y; } }
            public double displace_y { get { return transform.displace_y; } }
            public double displace_x { get { return transform.displace_x; } }
            public void save() { saved_transform = transform.Copy(); }
            public void restore() { System.Diagnostics.Trace.Assert(saved_transform != null); transform = saved_transform.Copy(); }
            public void apply_change(Transform transform_change) { transform += transform_change; }
            public (double, double) image_point_to_camera(double x, double y)
            {
                return transform.transform((x - 0.5 * width, y - 0.5 * height));
            }
            public (double, double) camera_to_image_point(double w, double h)
            {
                (double x, double y) = transform.untransform((w, h));
                return (x + 0.5 * width, y + 0.5 * height);
            }
            public int height { get; private set; }
            public int width { get; private set; }
            public CylinderImage(IImageD_Provider image_provider, double center_direction, double focal_length)
            {
                this.image_provider = image_provider;
                this.transform = new Transform(this) { center_direction = center_direction, focal_length = focal_length };
                this.image_provider.ImageDChanged += i => { this.height = i.height; this.width = i.width; };
            }
            public bool sample_pixel(double w, double h, out double r, out double g, out double b)
            {
                // h = y*(r/sqrt(x^2+f^2))
                // a = center_direction+atan(x/f)
                // r = 1, f fixed, for each "h, a", find "x, y"\
                MyImageD image = image_provider.GetImageD();
                (double x, double y) = camera_to_image_point(w, h);
                return image.sample(x, y, out r, out g, out b);
            }
        }
    }
}
