namespace CatPrinterBLE;

class Bayer2x2Dither : OrderedDither
{
    public Bayer2x2Dither() : base(bayer2x2Matrix, 2, 2, 4)
    {

    }

    static readonly byte[,] bayer2x2Matrix = new byte[,]
    {
        { 0, 2 },
        { 3, 1 },
    };
}