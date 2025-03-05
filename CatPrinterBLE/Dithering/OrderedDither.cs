namespace CatPrinterBLE;

abstract class OrderedDither : BaseDither
{
    readonly byte[,] matrix;
    readonly int matrixWidth;
    readonly int matrixHeight;
    readonly int numValues;

    protected OrderedDither(byte[,] matrix, int matrixWidth, int matrixHeight, int numValues)
    {
        this.matrix = matrix;
        this.matrixWidth = matrixWidth;
        this.matrixHeight = matrixHeight;
        this.numValues = numValues;
    }

    public override void Dither(float[] pixels, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = y * width + x;

                byte threshold = matrix[x & (matrixWidth - 1), y & (matrixHeight - 1)];
                float limit = (threshold + 1.0f) / (1.0f + numValues);
                pixels[offset] = pixels[offset] > limit ? 1f : 0f;
            }
        }
    }
}