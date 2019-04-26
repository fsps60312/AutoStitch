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
        private void InitializeViews()
        {
            this.Content = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(2,GridUnitType.Star)},
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)}
                },
                Children =
                {
                    new TabControl
                    {
                        Items =
                        {
                            new TabItem
                            {
                                Header="Test",
                                Content=new Pages.TestPage()
                            },
                            new TabItem
                            {
                                Header="Plane Spring",
                                Content=new Pages.PlaneSpringPage()
                            },
                            new TabItem
                            {
                                Header="Plane",
                                Content=new Pages.PlanePage()
                            },
                            new TabItem
                            {
                                Header="Cylinder",
                                Content=new Pages.CylinderPage()
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
            this.Closed += delegate { System.Diagnostics.Process.GetCurrentProcess().Kill(); };
            LogPanel.Log("ok.");
        }
    }
}
