using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.ImageD_Providers
{
    class ImageD_Cache : ImageD_Provider
    {
        Func<MyImageD> get_imaged;
        public ImageD_Cache(MyImageD imaged) { this.get_imaged = () => imaged; }
        public ImageD_Cache(Func<MyImageD> get_imaged) { this.get_imaged = get_imaged; }
        protected override MyImageD GetImageDInternal() { return get_imaged(); }
    }
}
