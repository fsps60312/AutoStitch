using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch
{
    public delegate void PointsChangedEventHandler(List<ImagePoint> points);
    public class ImagePoint:IComparable<ImagePoint>
    {
        public double x { get; private set; }
        public double y { get; private set; }
        public double importance { get; private set; }
        public object content { get; private set; }
        public ImagePoint(double x, double y, double importance, object content = null)
        {
            this.x = x;
            this.y = y;
            this.importance = importance;
            this.content = content;
        }
        public int CompareTo(ImagePoint other)
        {
            return importance.CompareTo(other.importance);
        }
    }
    public abstract class PointsProvider : IPointsProvider
    {
        public event PointsChangedEventHandler PointsChanged;
        private List<ImagePoint> ans = null;
        public virtual void Reset() { ResetSelf(); }
        public void ResetSelf() { ans = null; }
        public List<ImagePoint> GetPoints()
        {
            if (ans == null)
            {
                ans = GetPointsInternal();
                PointsChanged?.Invoke(ans);
            }
            return ans;
        }
        protected abstract List<ImagePoint>GetPointsInternal();
    }
    interface IPointsProvider
    {
        event PointsChangedEventHandler PointsChanged;
        List<ImagePoint> GetPoints();
        void Reset();
        void ResetSelf();
    }
}
