using System;

namespace CatPrinterBLE;

public static class Logger
{
    public static bool Verbose { get; set; } = false;

    public static void LogLine(string message, bool verbose=false)
    {
        if (verbose && !Verbose) return;
        Console.WriteLine($"{message}");
    }

    public static void LogLine()
    {
        Console.WriteLine();
    }

    public static void Log(string message, bool verbose = false)
    {
        if (verbose && !Verbose) return;
        Console.Write($"{message}");
    }
}