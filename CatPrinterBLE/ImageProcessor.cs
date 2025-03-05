using SkiaSharp;
using System;
using System.IO;

namespace CatPrinterBLE;

class ImageProcessor
{
    public const float LINEAR_GAMMA = 2.2f;

    public static byte[]? LoadAndProcess(string imagePath, int printWidth, float gamma = LINEAR_GAMMA, BaseDither.Methods ditheringMethod = BaseDither.Methods.FloydSteinberg)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            Console.WriteLine("The specified image path is not valid.");
            return null;
        }

        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"The specified image path doesn't exist.");
            return null;
        }

        using SKBitmap bitmap = SKBitmap.Decode(imagePath);

        // Create grayscale image resized to the printer's appropriate size

        float aspectRatio = (float)bitmap.Width / bitmap.Height;
        SKImageInfo info = new SKImageInfo(printWidth, (int)(printWidth / aspectRatio), SKColorType.Gray8, SKAlphaType.Opaque);
        using SKBitmap newBitmap = bitmap.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell));

        Span<byte> pixels = newBitmap.GetPixelSpan();

        // Dither the image if requested

        BaseDither? dither = BaseDither.Get(ditheringMethod);
        if (dither != null)
        {
            // Convert manually from sRGB bytes to linear floats to have accurate dithering results.
            // Didn't find out a way of doing this with SkiaSharp, so let's do it manually and inefficiently for now :).

            float[] pixelsF = new float[newBitmap.Width * newBitmap.Height];
            for (int p = 0; p < pixelsF.Length; p++) pixelsF[p] = MathF.Pow(pixels[p] / 255f, gamma);

            // Dither the image

            dither.Dither(pixelsF, newBitmap.Width, newBitmap.Height);

            // Convert back to sRGB bytes

            for (int p = 0; p < pixelsF.Length; p++) pixels[p] = (byte)(MathF.Pow(pixelsF[p], 1 / gamma) * 255f);
        }

#if DEBUG
        using (FileStream fs = File.Create("DitheredImage2.png"))
        {
            newBitmap.Encode(fs, SKEncodedImageFormat.Png, 100);
        }
#endif

        // Convert to 1bpp

        byte[] bytes = new byte[(newBitmap.Width * newBitmap.Height) >> 3];
        for (int b = 0; b < bytes.Length; b++)
        {
            byte bits = 0;
            int p = b << 3;
            if (pixels[p + 0] < 128) bits |= (1 << 0);
            if (pixels[p + 1] < 128) bits |= (1 << 1);
            if (pixels[p + 2] < 128) bits |= (1 << 2);
            if (pixels[p + 3] < 128) bits |= (1 << 3);
            if (pixels[p + 4] < 128) bits |= (1 << 4);
            if (pixels[p + 5] < 128) bits |= (1 << 5);
            if (pixels[p + 6] < 128) bits |= (1 << 6);
            if (pixels[p + 7] < 128) bits |= (1 << 7);
            bytes[b] = bits;
        }

        return bytes;
    }
}