using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.ImageD_Providers
{
    class Blend : ImageD_Provider
    {
        IImageD_Provider provider_base;
        IImageD_Provider[] providers;
        /// <summary>
        /// weighted sum according to images' alphas, resulted alpha will always be 1, or 0 if weights are all 0
        /// </summary>
        /// <param name="provider_base"></param>
        /// <param name="providers"></param>
        public Blend(IImageD_Provider provider_base, params IImageD_Provider[] providers)
        {
            this.provider_base = provider_base;
            this.providers = providers;
        }
        private void scale_rgb_by_alpha(MyImageD img)
        {
            for (int i = 0; i < img.height; i++)
            {
                for (int j = 0; j < img.width; j++)
                {
                    int k = i * img.stride + j * 4;
                    double alpha = img.data[k + 3];
                    //bgra
                    img.data[k + 0] /= alpha;
                    img.data[k + 1] /= alpha;
                    img.data[k + 2] /= alpha;
                }
            }
        }
        private void normalize_alpha(MyImageD img)
        {
            for (int i = 0; i < img.height; i++)
            {
                for (int j = 0; j < img.width; j++)
                {
                    int k = i * img.stride + j * 4;
                    //bgra
                    double alpha = img.data[k + 3];
                    if (alpha != 0)
                    {
                        img.data[k + 0] /= alpha;
                        img.data[k + 1] /= alpha;
                        img.data[k + 2] /= alpha;
                        img.data[k + 3] = 1; // alpha
                    }
                }
            }
        }
        protected override MyImageD GetImageDInternal()
        {
            MyImageD img_ans = new MyImageD(provider_base.GetImageD());
            scale_rgb_by_alpha(img_ans);
            foreach (var provider in providers)
            {
                MyImageD img = new MyImageD(provider.GetImageD());
                System.Diagnostics.Trace.Assert(img.width == img_ans.width && img.height == img_ans.height && img.stride == img_ans.stride);
                scale_rgb_by_alpha(img);
                for (int i = 0; i < img.height; i++)
                {
                    for(int j = 0; j < img.width; j++)
                    {
                        int k = i * img.stride + j * 4;
                        img_ans.data[k + 0] += img.data[k + 0];
                        img_ans.data[k + 1] += img.data[k + 1];
                        img_ans.data[k + 2] += img.data[k + 2];
                        img_ans.data[k + 3] += img.data[k + 3];
                    }
                }
            }
            normalize_alpha(img_ans);
            return img_ans;
        }
        public override void Reset()
        {
            base.Reset();
            provider_base.Reset();
            foreach (var provider in providers) provider.Reset();
        }
    }
}
