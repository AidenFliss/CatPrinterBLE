using System;

namespace CatPrinterBLE;

class FloydSteinbergDither : BaseDither
{
    public override void Dither(float[] pixels, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = y * width + x;

                float newC = (byte)(pixels[offset] > 0.5f ? 1f : 0f);
                float error = pixels[offset] - newC;
                pixels[offset] = newC;

                if (x + 1 < width)
                {
                    offset = y * width + (x + 1);
                    pixels[offset] = Math.Clamp(pixels[offset] + (error * (7f / 16f)), 0f, 1f);

                    if (y + 1 < height)
                    {
                        offset = (y + 1) * width + (x + 1);
                        pixels[offset] = Math.Clamp(pixels[offset] + (error * (1f / 16f)), 0f, 1f);
                    }
                }

                if (y + 1 < height)
                {
                    offset = (y + 1) * width + x;
                    pixels[offset] = Math.Clamp(pixels[offset] + (error * (5f / 16f)), 0f, 1f);

                    if (x - 1 >= 0)
                    {
                        offset = (y + 1) * width + (x - 1);
                        pixels[offset] = Math.Clamp(pixels[offset] + (error * (3f / 16f)), 0f, 1f);
                    }
                }
            }
        }
    }
}