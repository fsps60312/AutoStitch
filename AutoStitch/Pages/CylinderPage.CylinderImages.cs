﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoStitch.Pages
{
    partial class CylinderPage
    {
        class CylinderImages : ImageD_Provider
        {
            protected List<IImageD_Provider> image_providers;
            public List<CylinderImage> cylinder_images { get; private set; }
            int width, height;
            public CylinderImages(List<IImageD_Provider> image_providers, int width, int height)
            {
                this.image_providers = image_providers;
                this.cylinder_images = image_providers.Select(p => new CylinderImage(p, Utils.RandDouble() * 2.0 * Math.PI, 100)).ToList();
                this.width = width;
                this.height = height;
            }
            protected override MyImageD GetImageDInternal()
            {
                MyImageD image = new MyMatrix(new double[height, width]).ToGrayImageD();
                double min_h = cylinder_images.Min(i => (i.displace_y - 0.5 * i.height) / i.focal_length),
                    max_h = cylinder_images.Max(i => (i.displace_y + 0.5 * i.height) / i.focal_length);
                System.Threading.Tasks.Parallel.For(0, height, i =>
                  {
                      for (int j = 0; j < width; j++)
                      {
                          double r = 0, g = 0, b = 0;
                          int cnt = 0;
                          foreach (var img in cylinder_images)
                          {
                              if (img.sample_pixel(((double)j / width) * 2.0 * Math.PI, (i * max_h + (height - 1 - i) * min_h) / (height - 1), out double _r, out double _g, out double _b))
                              {
                                  r += _r; g += _g; b += _b;
                                  cnt++;
                              }
                          }
                          if (cnt > 0) { r /= cnt; g /= cnt; b /= cnt; }
                          int k = i * image.stride + j * 4;
                          // bgra
                          image.data[k + 0] = b;
                          image.data[k + 1] = g;
                          image.data[k + 2] = r;
                          image.data[k + 3] = 1;
                      }
                  });
                return image;
            }
            public override void Reset()
            {
                base.Reset();
                foreach (var provider in image_providers) provider.Reset();
            }
        }
    }
}
