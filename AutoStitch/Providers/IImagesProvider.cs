using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch
{
    interface IImagesProvider
    {
        List<MyImage> GetImages();
    }
}
