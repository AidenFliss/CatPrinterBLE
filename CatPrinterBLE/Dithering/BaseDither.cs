namespace CatPrinterBLE;

abstract class BaseDither
{
    public enum Methods
    {
        None,
        Bayer2x2,
        Bayer4x4,
        Bayer8x8,
        Bayer16x16,
        BlueNoise,
        FloydSteinberg
    }

    /// <summary>
    /// It dithers a grayscale image.
    /// </summary>
    /// <param name="pixels">Array of grayscale pixels in linear space.</param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    public abstract void Dither(float[] pixels, int width, int height);

    public static BaseDither? Get(Methods method)
    {
        switch (method)
        {
            case Methods.Bayer2x2: return new Bayer2x2Dither();
            case Methods.Bayer4x4: return new Bayer4x4Dither();
            case Methods.Bayer8x8: return new Bayer8x8Dither();
            case Methods.Bayer16x16: return new Bayer16x16Dither();
            case Methods.BlueNoise: return new BlueNoiseDither();
            case Methods.FloydSteinberg: return new FloydSteinbergDither();
        }

        return null;
    }
}