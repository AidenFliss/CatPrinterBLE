namespace CatPrinterBLE;

class Bayer4x4Dither : OrderedDither
{
    public Bayer4x4Dither() : base(bayer4x4Matrix, 4, 4, 16)
    {

    }

    static readonly byte[,] bayer4x4Matrix = new byte[,]
    {
        { 0, 8, 2, 10 },
        { 12, 4, 14, 6 },
        { 3, 11, 1, 9 },
        { 15, 7, 13, 5 },
    };
}