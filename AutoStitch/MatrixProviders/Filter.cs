namespace AutoStitch.MatrixProviders
{
    class Filter : MatrixProvider
    {
        public static Filter Red(IImageD_Provider provider) { return new Filter(provider, ColorChannel.Red); }
        public static Filter Green(IImageD_Provider provider) { return new Filter(provider, ColorChannel.Green); }
        public static Filter Blue(IImageD_Provider provider) { return new Filter(provider, ColorChannel.Blue); }
        public static Filter Alpha(IImageD_Provider provider) { return new Filter(provider, ColorChannel.Alpha); }
        public enum ColorChannel { Blue = 0, Green = 1, Red = 2, Alpha = 3 }
        IImageD_Provider provider;
        int color_channel;
        private Filter(IImageD_Provider provider, ColorChannel color_channel)
        {
            this.provider = provider;
            this.color_channel = (int)color_channel;
        }
        public override void Reset()
        {
            base.Reset();
            provider.Reset();
        }
        protected override MyMatrix GetMatrixInternal()
        {
            var image = provider.GetImageD();
            double[,] data = new double[image.height, image.width];
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    data[i, j] = image.data[i * image.stride + j * 4 + color_channel];
                }
            }
            return new MyMatrix(data);
        }
    }
}
