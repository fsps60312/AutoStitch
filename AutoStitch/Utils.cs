using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch
{
    public static class Utils
    {
        public static bool AllTheSame(params object[] objects)
        {
            for (int i = 1; i < objects.Length; i++) if (!objects[0].Equals(objects[i])) return false;
            return true;
        }
        static Random rand = new Random();
        public static double RandDouble() { return rand.NextDouble(); }
        /// <summary>
        /// return a number between [minValue, maxValue-1]
        /// </summary>
        /// <param name="minValue">inclusive</param>
        /// <param name="maxValue">exclusive</param>
        /// <returns></returns>
        public static int Rand(int minValue,int maxValue) { return rand.Next(minValue, maxValue); }
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
        /// <summary>
        /// let the points vote for the best average point without outliners
        /// </summary>
        /// <param name="points">the points</param>
        /// <param name="tolerance">maximal distance that a point will accept a point</param>
        /// <param name="tries"></param>
        /// <param name="accept_ratio">minimum propotion of acceptance that a point is selected</param>
        /// <param name="accept_threshold">mimumum number of acceptances that a point is selected</param>
        /// <returns></returns>
        public static Tuple<double, double> Vote(List<Tuple<double, double>> points,double tolerance, out int max_num_inliners, int tries = 50)
        {
            max_num_inliners = 0;
            Tuple<double, double> candidate = null;
            var accepts = new Func<Tuple<double, double>, Tuple<double, double>, bool>((p, q) =>
                  {
                      double dx = p.Item1 - q.Item1;
                      double dy = p.Item2 - q.Item2;
                      double d = dx * dx + dy * dy;
                      return d <= tolerance * tolerance;
                  });
            for(int i=0;i<tries;i++)
            {
                var point = points[Rand(0, points.Count)];
                int num_inliners = points.Sum(p => accepts(p, point) ? 1 : 0);
                if (num_inliners > max_num_inliners) { max_num_inliners = num_inliners; candidate = point; }
            }
            System.Diagnostics.Trace.Assert(candidate != null);
            List<Tuple<double, double>> inliners = points.Where(p => accepts(p, candidate)).ToList();
            return new Tuple<double, double>(inliners.Sum(p => p.Item1) / inliners.Count, inliners.Sum(p => p.Item2) / inliners.Count);
        }
        public static List<int> VoteInliners(List<Tuple<double, double>> points, double tolerance, int tries = 50)
        {
            var accepts = new Func<Tuple<double, double>, Tuple<double, double>, bool>((p, q) =>
            {
                double dx = p.Item1 - q.Item1;
                double dy = p.Item2 - q.Item2;
                double d = dx * dx + dy * dy;
                return d <= tolerance * tolerance;
            });
            var selected_point = Vote(points, tolerance, out _, tries);
            List<int> ans = new List<int>();
            for (int i = 0; i < points.Count; i++) if (accepts(points[i], selected_point)) ans.Add(i);
            return ans;
        }
        public static double Mod2PI(double v)
        {
            v %= 2.0 * Math.PI;
            if (v < 0) v += 2.0 * Math.PI;
            return v;
        }
    }
}
