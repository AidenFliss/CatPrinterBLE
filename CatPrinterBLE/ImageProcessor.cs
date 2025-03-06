using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Dithering;
using System;
using System.IO;
using System.Numerics;

namespace CatPrinterBLE;

class ImageProcessor
{
    public enum ColorModes
    {
        Mode_1bpp,
        Mode_4bpp
    }

    public enum DitheringMethods
    {
        None,
        Bayer2x2,
        Bayer4x4,
        Bayer8x8,
        Bayer16x16,
        FloydSteinberg
    }

    public const float LINEAR_GAMMA = 2.2f;

    public static byte[]? LoadAndProcess(string imagePath, int printWidth, ColorModes colorMode = ColorModes.Mode_1bpp, DitheringMethods ditheringMethod = DitheringMethods.None)
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

        //imagePath = @"D:\Proyectos\.NET\Cat Printer\Skia\Gradient.png";
        //colorMode = ColorModes.Mode_1bpp;
        //ditheringMethod = BaseDither.Methods.Bayer8x8;

        using Image<L8> image = Image.Load<L8>(imagePath);

        float aspectRatio2 = (float)image.Width / image.Height;
        image.Mutate(i => i.Resize(printWidth, (int)(printWidth / aspectRatio2), KnownResamplers.Lanczos3));

        IDither dither;
        float scale;
        switch (ditheringMethod)
        {
            // Scale has to be lower for Ordered dithering methods as it generate wrong colors otherwise.
            // 0.2 is a value I eyeballed that generates a result perceptually similar to the original image.

            case DitheringMethods.Bayer2x2: dither = KnownDitherings.Bayer2x2; scale = 0.2f; break;
            case DitheringMethods.Bayer4x4: dither = KnownDitherings.Bayer4x4; scale = 0.2f; break;
            case DitheringMethods.Bayer8x8: dither = KnownDitherings.Bayer8x8; scale = 0.2f; break;
            case DitheringMethods.Bayer16x16: dither = KnownDitherings.Bayer16x16; scale = 0.2f; break;
            default: dither = KnownDitherings.FloydSteinberg; scale = 1.0f; break;
        }

        byte[] bytes;

        if (colorMode == ColorModes.Mode_1bpp)
        {
            if (ditheringMethod == DitheringMethods.None)
            {
                image.Mutate(x => x.BinaryThreshold(0.5f, BinaryThresholdMode.Luminance));
            }
            else
            {
                image.Mutate(x => x
                .ProcessPixelRowsAsVector4(row =>
                {
                    for (int x = 0; x < row.Length; x++)
                    {
                        float color = MathF.Pow(row[x].X, LINEAR_GAMMA);
                        row[x] = new Vector4(color, color, color, row[x].W);
                    }
                })
                .BinaryDither(dither));
            }

            bytes = new byte[(image.Width * image.Height) >> 3];

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<L8> pixelRow = accessor.GetRowSpan(y);

                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        byte pixel = (byte)((255 - pixelRow[x].PackedValue) >> 7);
                        bytes[(y * accessor.Width + x) >> 3] |= (byte)(pixel << (x & 7));
                    }
                }
            });
        }
        else
        {
            if (ditheringMethod != DitheringMethods.None)
            {
                Color[] colors = new Color[16];
                for (int c = 0; c < colors.Length; c++)
                {
                    float color = (float)c / (colors.Length - 1);
                    colors[c] = new Color(new Vector4(color, color, color, 1.0f));
                }

                image.Mutate(x => x.Dither(dither, scale, colors));
            }

            bytes = new byte[(image.Width * image.Height) >> 1];

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<L8> pixelRow = accessor.GetRowSpan(y);

                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        byte pixel = (byte)((255 - pixelRow[x].PackedValue) >> 4);
                        bytes[(y * accessor.Width + x) >> 1] |= (byte)(pixel << ((x & 1) << 4));
                    }
                }
            });
        }

#if DEBUG
        image.Save("DitheredImage2.png");
#endif

        return bytes;
    }
}