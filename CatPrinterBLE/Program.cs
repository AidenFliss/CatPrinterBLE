using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CatPrinterBLE;

internal class Program
{
    static async Task Main(string[] args)
    {
        ShowHeader();

        //await using (CatPrinterBle ble = new CatPrinterBle())
        //{
        //    bool success = await ble.ConnectAsync();

        //    if (success) await ble.GetPrinterStatusAsync();
        //    if (success)
        //    {
        //        await ble.SetPrintIntensity(69);
        //        await ble.SetPrintIntensity(26);
        //    }
        //}

        //return;

        if (args.Length < 1)
        {
            ShowUsage();
            return;
        }

        switch (args[0])
        {
            case "-di":
            case "--deviceInfo":

                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.PrintDeviceInfoAsync();
                }

                break;

            case "-ps":
            case "--printerStatus":

                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.GetPrinterStatusAsync();
                }

                break;

            case "-bl":
            case "--batteryLevel":

                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.GetBatteryLevelAsync();
                }

                break;

            case "-qc":
            case "--queryCount":

                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.GetQueryCount();
                }

                break;

            case "-p":
            case "--print":

                if (args.Length < 5)
                {
                    ShowUsage();
                    return;
                }

                if (!byte.TryParse(args[1], out byte intensity))
                {
                    intensity = 50;
                }

                CatPrinter.PrintModes printMode;
                switch (args[2])
                {
                    case "4bpp": printMode = CatPrinter.PrintModes.Grayscale; break;
                    default: printMode = CatPrinter.PrintModes.Monochrome; break;
                }

                BaseDither.Methods ditheringMethod;
                switch (args[3])
                {
                    case "Bayer2x2": ditheringMethod = BaseDither.Methods.Bayer2x2; break;
                    case "Bayer4x4": ditheringMethod = BaseDither.Methods.Bayer4x4; break;
                    case "Bayer8x8": ditheringMethod = BaseDither.Methods.Bayer8x8; break;
                    case "Bayer16x16": ditheringMethod = BaseDither.Methods.Bayer16x16; break;
                    case "BlueNoise": ditheringMethod = BaseDither.Methods.BlueNoise; break;
                    case "FloydSteinberg": ditheringMethod = BaseDither.Methods.FloydSteinberg; break;
                    default: ditheringMethod = BaseDither.Methods.None; break;
                }

                string imagePath = args[4];

                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success)
                    {
                        await ble.SetPrintIntensity(intensity);
                        await ble.Print(imagePath, printMode, ditheringMethod);
                    }
                }

                break;
        }

        //Console.ReadKey();
    }

    static void ShowHeader()
    {
        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        string v = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "?.?.?";

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine();
        Console.WriteLine("        #----------------------------------------------------------------#");

        Console.WriteLine("        #                   Cat Printer - Version " + v + "                  #");
        Console.Write("        #             ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("https://github.com/MaikelChan/AFSPacker");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("            #");

        Console.WriteLine("        #                                                                #");
        Console.WriteLine("        #                    By MaikelChan / PacoChan                    #");
        Console.WriteLine("        #----------------------------------------------------------------#\n");

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("        This program provides basic functionality to use one of the most");
        Console.WriteLine("        recent models (as of March 2025) of Cat Printers: Model MXW01.\n\n");
    }

    static void ShowUsage()
    {
        Console.ForegroundColor = ConsoleColor.Gray;

        Console.WriteLine("Usage:\n");

        Console.ForegroundColor = ConsoleColor.White;

        Console.WriteLine("  AFSPacker -e <input_afs_file> <output_dir>  :  Extract AFS archive");
        Console.WriteLine("  AFSPacker -c <input_dir> <output_afs_file>  :  Create AFS archive");
        Console.WriteLine("  AFSPacker -i <input_afs_file>               :  Show AFS information\n");
    }
}