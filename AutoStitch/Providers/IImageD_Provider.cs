using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch
{
    delegate void ImageDChangedEventHandler(MyImageD image);
    interface IImageD_Provider
    {
        event ImageDChangedEventHandler ImageDChanged;
        MyImageD GetImageD();
        void Reset();
    }
    abstract class ImageD_Provider : IImageD_Provider
    {
        public event ImageDChangedEventHandler ImageDChanged;
        private MyImageD ans = null;
        public virtual void Reset() { ans = null; }
        public MyImageD GetImageD()
        {
            if (ans == null)
            {
                ans = GetImageDInternal();
                ImageDChanged?.Invoke(ans);
            }
            return ans;
        }
        protected abstract MyImageD GetImageDInternal();
    }
}
