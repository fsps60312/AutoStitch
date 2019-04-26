using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch
{
    public delegate void PointsChangedEventHandler(List<ImagePoint> points);
    public delegate void PointsChangedEventHandler<T>(List<ImagePoint<T>> points);
    public class ImagePoint : IComparable<ImagePoint>
    {
        public double x { get; private set; }
        public double y { get; private set; }
        public double importance { get; private set; }
        public ImagePoint(double x, double y, double importance)
        {
            this.x = x;
            this.y = y;
            this.importance = importance;
        }
        public int CompareTo(ImagePoint other)
        {
            return importance.CompareTo(other.importance);
        }
    }
    public class ImagePoint<T> :ImagePoint
    {
        public T content { get; private set; }
        public ImagePoint(double x, double y, double importance, T content = default(T)):base(x,y,importance)
        {
            this.content = content;
        }
    }
    public abstract class PointsProvider: IPointsProvider
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
        protected abstract List<ImagePoint> GetPointsInternal();
    }
    public abstract class PointsProvider<T> : PointsProvider, IPointsProvider<T>
    {
        public new event PointsChangedEventHandler<T> PointsChanged;
        private List<ImagePoint<T>> ans = null;
        List<ImagePoint<T>> IPointsProvider<T>.GetPoints()
        {
            if (ans == null)
            {
                ans = GetPointsInternal().Cast<ImagePoint<T>>().ToList();
                PointsChanged?.Invoke(ans);
            }
            return ans;
        }
    }
    interface IPointsProvider
    {
        event PointsChangedEventHandler PointsChanged;
        List<ImagePoint> GetPoints();
        void Reset();
        void ResetSelf();
    }
    interface IPointsProvider<T>:IPointsProvider
    {
        new event PointsChangedEventHandler<T> PointsChanged;
        new List<ImagePoint<T>> GetPoints();
        new void Reset();
        new void ResetSelf();
    }
}
