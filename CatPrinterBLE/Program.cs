using System;
using System.Reflection;
using System.Threading.Tasks;
using static CatPrinterBLE.ImageProcessor;

namespace CatPrinterBLE;

internal class Program
{
    static async Task Main(string[] args)
    {
        ShowHeader();

        if (args.Length < 1)
        {
            ShowUsage();
            return;
        }

        switch (args[0])
        {
            case "-p":
            case "--print":

                if (args.Length < 5)
                {
                    ShowUsage();
                    return;
                }

                if (!byte.TryParse(args[1], out byte intensity))
                {
                    Console.WriteLine($"Error: Invalid intensity ({args[1]}).");
                    return;
                }

                CatPrinter.PrintModes printMode;
                switch (args[2])
                {
                    case "1bpp": printMode = CatPrinter.PrintModes.Monochrome; break;
                    case "4bpp": printMode = CatPrinter.PrintModes.Grayscale; break;
                    default: Console.WriteLine($"Error: Invalid print mode ({args[2]})."); return;
                }

                DitheringMethods ditheringMethod;
                switch (args[3])
                {
                    case "None": ditheringMethod = DitheringMethods.None; break;
                    case "Bayer2x2": ditheringMethod = DitheringMethods.Bayer2x2; break;
                    case "Bayer4x4": ditheringMethod = DitheringMethods.Bayer4x4; break;
                    case "Bayer8x8": ditheringMethod = DitheringMethods.Bayer8x8; break;
                    case "Bayer16x16": ditheringMethod = DitheringMethods.Bayer16x16; break;
                    case "FloydSteinberg": ditheringMethod = DitheringMethods.FloydSteinberg; break;
                    default: Console.WriteLine($"Error: Invalid dithering method ({args[3]})."); return;
                }

                string imagePath = args[4];

                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success)
                    {
                        await ble.Print(imagePath, intensity, printMode, ditheringMethod);
                    }
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

            case "-di":
            case "--deviceInfo":

                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.PrintDeviceInfoAsync();
                }

                break;

            case "-pt":
            case "--printType":

                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.GetPrintType();
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

            default:

                ShowUsage();
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

        Console.WriteLine("        #                 Cat Printer BLE - Version " + v + "                #");
        Console.Write("        #           ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("https://github.com/MaikelChan/CatPrinterBLE");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("          #");

        Console.WriteLine("        #                                                                #");
        Console.WriteLine("        #                    By MaikelChan / PacoChan                    #");
        Console.WriteLine("        #----------------------------------------------------------------#\n");

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("        This program provides basic functionality to use one of the most");
        Console.WriteLine("        recent models (as of March 2025) of Cat Printers: Model MXW01.\n");

        Console.WriteLine("        It can load any image, and it will resize it to the proper resolution");
        Console.WriteLine("        and apply a dithering patten to smooth the gradients after the color reduction.\n\n");
    }

    static void ShowUsage()
    {
        Console.ForegroundColor = ConsoleColor.Gray;

        Console.WriteLine("Usage:\n");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  CatPrinterBLE (-p  | --print) <intensity> <print_mode> <dithering_method> <image_path>\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("    Prints the specified image.\n");
        Console.WriteLine("    Example:");
        Console.WriteLine("      CatPrinterBLE -p 100 1bpp FloydSteinberg \"C:\\CoolCat.png\"\n");
        Console.WriteLine("    Parameters:");
        Console.WriteLine("      - intensity        : How dark the printing will be. Values from 0 to 100.");
        Console.WriteLine("      - print_mode       : The amount of colors that will be used for printing.");
        Console.WriteLine("                           Possible values:");
        Console.WriteLine("                             - 1bpp: Monochrome, pure black and white. Faster printing, lower quality.");
        Console.WriteLine("                             - 4bpp: 16bit grayscale. Slower printing, higher quality.");
        Console.WriteLine("      - dithering_method : The dithering pattern that will be used for the color reduction.");
        Console.WriteLine("                           Possible values:");
        Console.WriteLine("                             - Bayer2x2");
        Console.WriteLine("                             - Bayer4x4");
        Console.WriteLine("                             - Bayer8x8");
        Console.WriteLine("                             - Bayer16x16");
        Console.WriteLine("                             - FloydSteinberg");
        Console.WriteLine("      - image_path       : The path to the image to print.\n");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  CatPrinterBLE (-ps | --printerStatus)\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("    Prints the current status of the printer, for example, if it has paper, current temperature and battery level.\n");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  CatPrinterBLE (-bl | --batteryLevel)\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("    Shows the current battery level. This can also be shown with the -ps command.\n");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  CatPrinterBLE (-di | --deviceInfo)\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("    Prints some device information. Useful for debugging.\n");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  CatPrinterBLE (-pt | --printType)\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("    This returns some information abou the \"print type\" or maybe \"printer type\".");
        Console.WriteLine("    I still haven't figured out what this means exactly. Types are decompiled phonetically written Chinese.");
        Console.WriteLine("    This can also be obtained with the -ps command.\n");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  CatPrinterBLE (-qc | --queryCount)\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("    No idea what is this, but the printer supports this command that returns some FF values.\n");
    }
}