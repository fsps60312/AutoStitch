using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch
{
    delegate void ImageChangedEventHandler(MyImage image);
    interface IImageProvider
    {
        event ImageChangedEventHandler ImageChanged;
        MyImage GetImage();
        void Reset();
        void ResetSelf();
    }
#if false
    abstract class ImageProvider : IImageProvider
    {
        public event ImageChangedEventHandler ImageChanged;
        private MyImage ans = null;
        public virtual void Reset() { ResetSelf(); }
        public void ResetSelf() { ans = null; }
        public MyImage GetImage()
        {
            if (ans == null)
            {
                ans = GetImageInternal();
                ImageChanged?.Invoke(ans);
            }
            return ans;
        }
        protected abstract MyImage GetImageInternal();
    }
#endif
}
