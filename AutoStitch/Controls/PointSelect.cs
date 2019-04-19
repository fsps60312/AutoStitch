using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media;

namespace AutoStitch.Controls
{
    public delegate void PointSelectedEventHandler(ImagePoint p);
    interface IPointSelect
    {
        event PointSelectedEventHandler PointSelected;
    }
    class PointSelect :ContentControl , IPointSelect
    {
        public List<ImagePoint> points { get; private set; } = null;
        Image image = new Image();
        Line horizontal_line = new Line(), vertical_line = new Line();
        public void ShowPoint(double x,double y)
        {
            selected_x = x;
            selected_y = y;
            DrawDashLines(false);
        }
        private void Image_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!is_mouse_in) return;
            Point mouse_position = e.GetPosition(image);
            mouse_position.X /= image.ActualWidth;
            mouse_position.Y /= image.ActualHeight;
            selected_x = mouse_position.X * image_width;
            selected_y = mouse_position.Y * image_height;
            //LogPanel.Log(mouse_position.ToString());
            try
            {
                if (points == null) { LogPanel.Log("no points selected."); }
                ImagePoint nearest_point = points[0];
                foreach (var p in points)
                {
                    if (Math.Pow(p.x - selected_x, 2) + Math.Pow(p.y - selected_y, 2) < Math.Pow(nearest_point.x - selected_x, 2) + Math.Pow(nearest_point.y - selected_y, 2)) nearest_point = p;
                }
                selected_x = nearest_point.x;
                selected_y = nearest_point.y;
                LogPanel.Log($"selected: x = {nearest_point.x}, y = {nearest_point.y}, importance = {nearest_point.importance}.");
                PointSelected?.Invoke(nearest_point);
            }
            finally { DrawDashLines(); }
        }
        double
            selected_x = 0.5,
            selected_y = 0.5,
            image_width = 1,
            image_height = 1;
        private void DrawDashLines(bool passive=false)
        {
            double width = image.ActualWidth, height = image.ActualHeight;
            horizontal_line.X1 = 0;
            horizontal_line.X2 = width;
            horizontal_line.Y1 = horizontal_line.Y2 = height * selected_y / image_height;
            vertical_line.Y1 = 0;
            vertical_line.Y2 = height;
            vertical_line.X1 = vertical_line.X2 = width * selected_x / image_width;
            horizontal_line.Visibility = vertical_line.Visibility = Visibility.Visible;
            horizontal_line.Stroke = vertical_line.Stroke = new SolidColorBrush(!passive ? Colors.Red : Colors.Blue);
        }
        private void InitializeViews()
        {
            this.MinHeight = 300;
            image.Stretch = Stretch.UniformToFill;
            image.StretchDirection = StretchDirection.Both;
            this.Content = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)},
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Auto)},
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)}
                },
                RowDefinitions =
                {
                    new RowDefinition{Height=new GridLength(1,GridUnitType.Star)},
                    new RowDefinition{Height=new GridLength(500,GridUnitType.Pixel)},
                    new RowDefinition{Height=new GridLength(1,GridUnitType.Star)}
                },
                Children =
                {
                    image.Set(1,1),
                    horizontal_line.Set(1,1),
                    vertical_line.Set(1,1)
                }
            };
            horizontal_line.StrokeThickness = vertical_line.StrokeThickness = 1;
            horizontal_line.StrokeDashArray = vertical_line.StrokeDashArray = new DoubleCollection(new double[] { 3, 3 });
            horizontal_line.StrokeDashCap = vertical_line.StrokeDashCap = PenLineCap.Flat;
            horizontal_line.Visibility = vertical_line.Visibility = Visibility.Hidden;
        }
        private void line_pass_event_to_image(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            image.RaiseEvent(e);
        }

        bool is_mouse_in = false;
        private void RegisterEvents()
        {
            horizontal_line.MouseDown += line_pass_event_to_image;
            vertical_line.MouseDown += line_pass_event_to_image;
            horizontal_line.MouseUp += line_pass_event_to_image;
            vertical_line.MouseUp += line_pass_event_to_image;
            image.SizeChanged += Image_SizeChanged;
            image.MouseDown += delegate { is_mouse_in = true; };
            image.MouseLeave += delegate { is_mouse_in = false; };
            image.MouseUp += Image_MouseUp;
        }

        public PointSelect(IImageD_Provider image_provider, IPointsProvider points_provider)
        {
            IImageD_Provider provider = new ImageD_Providers.PlotPoints(image_provider, points_provider);
            image_provider.ImageDChanged += img => { provider.ResetSelf(); provider.GetImageD(); };
            points_provider.PointsChanged += ps => { this.points = ps; provider.ResetSelf(); provider.GetImageD(); };
            provider.ImageDChanged += img => { image_width = img.width; image_height = img.height; image.Source = img.ToImage().ToBitmapSource();};
            InitializeViews();
            RegisterEvents();
        }
        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (horizontal_line.Visibility == Visibility.Visible) DrawDashLines();
        }
        public event PointSelectedEventHandler PointSelected;
    }
}
