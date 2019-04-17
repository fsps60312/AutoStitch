using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch
{
    public static class Utils
    {
        public static void GetHeatColor(double v, out double r, out double g, out double b)
        {
            if (v < 0) r = g = b = 0;
            else if (v <= 0.25)
            {
                r = 0;
                g = v / 0.25;
                b = 1;
            }
            else if (v <= 0.5)
            {
                r = 0;
                g = 1;
                b = (0.5 - v) / 0.25;
            }
            else if (v <= 0.75)
            {
                r = ((v - 0.5) / 0.25 * 256).ClampByte();
                g = 1;
                b = 0;
            }
            else if (v <= 1)
            {
                r = 1;
                g = (1 - v) / 0.25;
                b = 0;
            }
            else r = g = b = 1;
        }
    }
}
